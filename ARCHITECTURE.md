# Architecture

The technical companion to the README: the rules the code holds itself to, how the pieces talk to
each other, the data model, and the algorithms worth explaining. What a production version would add
on top is in the `Scope and future work` section of `README.md`.

---

## Three rules the rest follows from

### 1. Core cannot reference the engine

`Assets/Scripts/Core/` is a separate assembly marked **No Engine References**. `using UnityEngine`
there does not fail review, it fails to compile. `UnityEngine.Random` is unreachable for the same
reason, so randomness is injected as a `System.Random` and every board is reproducible from a seed.

The payoff is the test suite: 65 cases that assert on rules, with no scene, no `GameObject` and no
play mode.

### 2. Logic resolves instantly, animation trails behind

`Board.TryBlast()` returns with the board in its final state — removals, Box damage, gravity, refill,
regrouping. The board is never observable half-resolved.

The view then walks blocks from where they were drawn to where the board says they now are. That
animation is **cosmetic**: it cannot change an outcome, and a dropped frame loses a sparkle, not a
move.

### 3. "Is this block still falling?" lives in the view

A tap on a cell whose *visual* block is mid-air is swallowed by `BoardView.TryPickCell`. Core has no
notion of time and never learns which blocks are in flight.

The filter asks about the tapped cell alone, never the group — landed blocks stay tappable while
others fall, so a group with one member mid-air must still blast.

---

## The layers

```
  Game (Unity)                              │  Core (no engine)
                                            │
  InputHandler ──► GameController ──────────┼──► GameSession ──► Board
                        │                   │                      ├─► GroupFinder
                        │ events            │                      ├─► GravityResolver
                        ▼                   │                      └─► DeadlockResolver
  BoardView, HudView, FeedbackView,         │
  LevelFlow, LevelBackground                │
  (read Core, never write it)               │
```

- **Core** owns every rule. `Board` owns the cells; `GameSession` owns what is true about one
  playthrough — moves, score, won or lost.
- **`GameController`** is the one place a `LevelConfig` becomes a Core board, and the only object
  that calls into Core during play. It holds no reference to anything that draws.
- **Views** subscribe to the controller and read Core; they never write it. `BoardView` draws the
  board, `HudView` the moves and the goal, `FeedbackView` the score and combo callouts.
- **`LevelFlow`** decides what the screen is doing — briefing, playing, paused, won — and switches
  input on and off. It is the only thing that opens popups in the level.

---

## A tap, end to end

This is the whole game loop, in the order the code runs it.

1. **`InputHandler.Update`** sees a press. It is enabled only while `PlayingState` is current, so a
   tap during the intro or under a popup never reaches the board.
2. **`BoardView.TryPickCell`** turns the screen position into a cell with arithmetic — no raycast. It
   refuses a tap outside the board, during a shuffle, or on a block still falling.
3. **`GameController.TryBlastAt`** hands the cell to **`GameSession.Play`**:
   - a lone block, a Box or a hole is **rejected** → `OnTapRejected`. The block wiggles, and
     `PlayingState` puts the hint away. Nothing in Core changed.
   - otherwise **`Board.TryBlast`** runs the whole move (see [Move order](#move-order)), then the
     session adds `size²` to the score, counts the move, and checks win, loss and deadlock.
4. **`OnBoardChanged(BlastResult)`** — `BoardView.ApplyBlast` catches up with the move Core already
   made: particles and sound, blasted blocks handed to the effect runner, surviving blocks sent
   falling to their new cells, new blocks spawned above the board, every sprite refreshed for its new
   icon tier. Each broken Box raises `BoardView.OnBoxBroken`, and the HUD flies a Box to the goal.
5. **`OnDeadlockResolved`**, only if the board had to be shuffled — the shrink-swap-grow animation.
6. **`OnStatusChanged`** — the HUD rewrites the moves and the goal, `FeedbackView` shows `+points`
   and a "Good / Great / Amazing" callout, and `LevelFlow` passes the new `GameState` to the current
   state.
7. If the level is decided, `PlayingState` moves to **`SettlingState`**, which waits until every
   block has landed and every flying Box has arrived, then a beat, then shows **won** or **lost**.
8. **`WonState`** saves the coins and the next level **on entry**, before anything celebrates — a
   player who kills the app during the confetti keeps the win. **Continue** returns home, where
   `CoinCounter` flies the reward into the coin pill.

---

## Scenes and the App

```
  App (created before any scene, DontDestroyOnLoad)
   ├── SceneTransition   fade out → load → fade in, on unscaled time
   └── AudioService      sound effects and music, carried across scenes

  Home.unity ── LevelRequest.Set(level, index) ──► App.LoadScene("Level") ──► Level.unity
  Level.unity ── WonState / LostState / ConfirmExitState ──► App.LoadScene("Home")
```

- **`App`** is instantiated from `Resources/App.prefab` by a `RuntimeInitializeOnLoadMethod`, before
  the first scene loads. Every scene can therefore be opened and played on its own in the editor and
  still find it, and no scene needs a destroy-the-duplicate check. `App.LoadScene` still loads a
  scene, without the fade, if the prefab is missing.
- **`LevelRequest`** is a ScriptableObject both scenes reference. Home writes the level to play; the
  level scene **takes** it on start, which clears it. Its fields are `NonSerialized`, so play mode
  never writes into the asset. With no request — the level scene opened directly — the controller
  plays its debug level and the campaign is not touched.
- **`PlayerProgress`** is the one door to saved state: level index, coins, the two sound switches.
  Nothing else knows that behind it is `SaveSystem` writing JSON. A save goes to a temporary file that
  is then swapped in, keeping the previous file as a backup, so an app killed mid-write loses at most
  the last save. It is written immediately on a win and again when the app is backgrounded.

---

## The level state machine

`LevelFlow` owns eight states, created once and reused, so changing state allocates nothing. It
forwards input, button clicks and the controller's status to whichever state is current; each state
overrides only the handful of those it answers.

```
  scene start
       │
       ▼
     Intro ──timer──► Playing ──won / lost──► Settling ──► Won ──continue──► Home
       ▲               │   ▲                      │
       │          pause│   │resume                └──────► Lost ──home──────► Home
       │               ▼   │                                 │
       │             Paused ──leave──► ConfirmExit ──► Home  │retry
       │               ▲                   │                 ▼
       │               └───────stay────────┘              Briefing ──close──► Home
       │                                                     │
       └─────────────────────────play────────────────────────┘
```

| State | On screen | Leaves when |
|---|---|---|
| `IntroState` | Blocks rain in, a banner states the goal. No input | Its timer runs out → Playing |
| `PlayingState` | The board takes taps. After five idle seconds the largest group pulses | Pause → Paused; Core reports won or lost → Settling |
| `PausedState` | Settings popup; `Time.timeScale = 0` freezes the board | Resume → Playing; Leave → ConfirmExit |
| `ConfirmExitState` | "Leave level?" over the frozen board | Stay → Paused; Leave → Home |
| `SettlingState` | Nothing new — the last move finishing | Board and HUD idle, plus a beat → Won or Lost |
| `WonState` | Flash, confetti, then the win card | Continue → Home |
| `LostState` | The lose card | Retry → Briefing; the home button → Home |
| `BriefingState` | The start popup again, after a retry | Play → Intro; Close → Home |

The first attempt at a level is briefed on the home screen before the scene loads, so the level scene
starts at `IntroState`. `BriefingState` exists for the retry, which regenerates the board in place —
no scene reload, no new pool.

Every button handler on a state begins with an `IsCurrent` check. Buttons are wired once for the whole level,
so a click can arrive while another state is current, and must then do nothing.

The popups themselves know nothing about the flow. `Popup` is a dimmed backdrop and a card that pops
in on unscaled time; each popup adds its labels and exposes its buttons, and the states decide which
one is open.

---

## Data model

```csharp
public enum CellType : byte { Empty = 0, Color = 1, Box = 2 }

public struct Cell {
    public CellType Type;
    public byte Color;    // only meaningful when Type is Color
    public byte Health;   // only meaningful when Type is Box
}

Cell[] cells;   // one dimension, M*N, row-major
```

`Cell` has only two legitimate shapes, so it is built through factories rather than a constructor: a
three-field constructor would allow a coloured Box.

`BoardConfig` is everything Core needs to build a board, and the one conversion point between Unity
and Core: `LevelConfig.ToBoardConfig()` produces it, and nothing else copies the values across.
`MoveLimit` and `Seed` are absent from it — neither shapes a board. It also holds the two rules the
view needs before or without a board, so they are written once:

- **`TierFor(size)`** — which icon a group of that size shows. `GroupFinder` uses it for every block's
  sprite, `FeedbackView` for the callout word.
- **`BoxesToPlace`** — the Box request clamped to `BoxCapacity`, every cell but the top row.
  Generation places exactly this many, and the start popup promises exactly this many, before the
  board exists.

`Grid` is stateless index arithmetic shared by `Board`, `GroupFinder` and `DeadlockResolver`.
`Grid.TryStep` checks bounds on `(row, col)` and never on `index ± 1`, which would connect the end of
one row to the start of the next.

### Conventions that will bite if forgotten

- **Row 0 is the bottom row**, so `worldY = origin.y + row * cellSize` needs no negation. Gravity
  falls toward decreasing index and new blocks enter from the highest.
- **`Cell` is a mutable struct in an array**, so the copy trap applies:
  ```csharp
  var c = cells[i]; c.Color = 2;   // wrong, mutates the copy
  cells[i].Color = 2;              // right
  ```
- **No sentinel colour for an empty cell.** `CellType.Empty` is separate, which it has to be — a Box
  has no colour either.
- **`groupIdOf` is filled with `-1`, not cleared.** Zero is a valid group id, so "no group" needs a
  different value.
- **`GroupFinder` holds no reference to the board.** It takes the cells per call as a
  `ReadOnlySpan<Cell>` and owns only its own scratch arrays.
- **Scratch arrays are class fields, allocated once** — `groupIdOf`, `groupSizes`, `stack`,
  `boxStamp`. Nothing on the gameplay path allocates.

---

## Move order

This sequence is a game rule, not an implementation detail:

1. Blast the group
2. Damage adjacent Boxes — **one per group**, not per neighbouring block
3. Gravity, then refill from above the column
4. Recalculate groups
5. Increment the move counter
6. **Won?** (no Boxes left)
7. **Lost?** (out of moves with a Box standing)
8. **Deadlocked?** → resolve

**6 must come before 7.** On the last move both can be true, and asking about the loss first would
take the win away on the move that earned it.

**8 comes last**, because shuffling a board that has already been won is an animation with nothing
behind it. A shuffle costs no move: the player neither caused it nor could avoid it.

### One invariant

Every method that changes the board ends by recalculating groups, `Generate()` included. No caller
has to remember to ask, and the board is never seen through stale group data.

---

## Algorithms

### Group finding

One full scan after every change answers three questions at once: what is blastable, what icon each
block shows, and whether the board is deadlocked (largest group < 2). Incremental would be wrong —
one column collapsing can merge or split groups several columns away.

Iterative DFS over an `int[]` stack, no recursion and no `Queue<T>`. **Cells are marked when pushed,
never when popped**: marking on pop would let all four neighbours push the same cell, and the stack
could exceed the cell count. Marking on push bounds it at exactly `M*N`.

The scan leaves a group id on every cell, which the view reuses: the hint finds the largest group's
members by comparing ids (`Board.GroupIdAt`) rather than running a search of its own.

### Box damage

A Box takes one damage per adjacent *group*, not per neighbouring block. A stamp compared against a
counter bumped once per blast does that with no set, no allocation and nothing to clear between
moves — old marks go stale by themselves.

### Gravity

Each column is split into segments by Boxes, which act as walls, and each segment collapses within
itself. Only the top segment refills.

A gap under a Box therefore stays empty, possibly for the rest of the level. **That is correct, not a
bug**, and it never locks a column: the topmost Box can always be damaged from above.

### Deadlock

Blind shuffle-and-recheck is ruled out by the case document, and it has no bound on how long it runs.
Instead: one pass that shuffles for the look of it, then places a group deliberately.

A single survey walk collects the coloured cells, the colour histogram, and — by reservoir sampling —
one adjacent pair. The sampled pair is the pair the guarantee step uses, which is what makes one pass
enough. Sampling rather than taking the first pair keeps the placed group out of the same corner
every time.

Two tiers, and only the colours ever move — Boxes, holes and Box health stay exactly where they are:

| Tier | When | What happens |
|---|---|---|
| 1 | Some colour occurs twice | The most common colour is **swapped** onto the sampled pair. Every colour count is preserved |
| 2 | No colour occurs twice | One cell of the pair is **assigned** its neighbour's colour. Exactly one cell changes |

Tier 2 exists because rearranging a set with no repeat still has no repeat, however it is ordered.
Small boards produce that routinely: a 2×2 holding one Box has three coloured cells, and with six
colours all three come out different more often than not.

**The one condition nothing can fix** is no two adjacent coloured cells — a colour cannot make two
cells neighbours. On a generated board that is unreachable, because the top row never holds a Box and
is therefore always a full run of colours.

### Board generation

Constrained random, and one constraint carries most of it: **no Box on the top row.** Every column can
then receive falling blocks → the top row is always full → two adjacent coloured cells always exist →
the resolver always has somewhere to place a group.

Generation also guarantees a legal move exists before play starts. It can do what the shuffle cannot:
the shuffle swaps and so needs a repeated colour, while generation assigns, so one write is enough
and cannot fail.

Boxes are placed by partial Fisher-Yates over the Box-eligible prefix of the array. Since row 0 is the
bottom and the array is row-major, "not the top row" costs no filtering.

---

## View layer

`BoardView` is the board's `MonoBehaviour` and holds the serialized settings; the work is split into
plain C# classes it creates once, at the first bind, and reuses across restarts:

| Class | Job |
|---|---|
| `BlockPool` ×2 | Every board block, and a separate set for effects — an effect holding a block must never starve the board of the one it is about to need |
| `FallAnimator` | Moves blocks to their new cells under gravity; knows which cells are settled |
| `EffectRunner` | Pops, shards, landing squashes, wiggles — on pooled sprites from the block atlas |
| `BoardHint` | Finds the largest group and pulses it until the player acts |
| `BoardDecor` | Cell tiles, the frame and trim, the mask — sized from the board, so any level fits |
| `BoardCamera` | Fits the camera to the board area, and shakes it |

- **`SpriteRenderer` plus one sprite atlas.** Everything on the board is in `BlockAtlas`, on one
  material. The board avoids Canvas, because a canvas rebuild is the cost worth avoiding when a
  hundred blocks move every frame.
- **Shards are pooled `SpriteRenderer`s, not particles**, which would draw with their own material
  and split the board's batch. The particles that do exist — sparkles, glows, splinters, dust,
  confetti — live in `Vfx`, all on one shared material, emitted on demand with no instance per burst.
- **Object pooling.** Every block is created at startup; no `Instantiate` or `Destroy` during play.
  The board's pool throws rather than growing — capacity comes from the board, so running out means
  the view leaked a block. The effect pool uses `TryRent` and drops the effect instead.
- **The camera is fitted to the board**, never the board scaled to the screen. Scale would leak into
  the input maths and into every fall distance. The region it fits into is an invisible `BoardArea`
  rectangle in the HUD, inside the safe area.
- **Input is arithmetic, not raycasting.** `ScreenToWorldPoint` and a floor division; there is not one
  collider on the board. The reason is correctness rather than speed — a raycast asks the visual
  world what was hit, and during a fall the visual world is deliberately behind the logical one.
- **One `Update` for the whole board.** `FallAnimator` and `EffectRunner` are preallocated struct
  arrays walked by a single tick; no block has an `Update` of its own.
- **Two animation systems, one rule.** The board animates through its own animators above. UI and
  meta — popups, banners, flying Boxes and coins, punches — use PrimeTween, on unscaled time so they
  keep moving while the game is paused, with target-overload callbacks so no tween captures a closure.

---

## Events

`GameController` raises five; it holds no reference to any listener.

```csharp
public event Action<Board> OnBoardReady;          // BoardView, HudView, FeedbackView, LevelBackground
public event Action<BlastResult> OnBoardChanged;  // BoardView, FeedbackView
public event Action OnDeadlockResolved;           // BoardView
public event Action OnStatusChanged;              // HudView, FeedbackView, LevelFlow
public event Action<int> OnTapRejected;           // BoardView, LevelFlow
```

`BoardView` raises one more, `OnBoxBroken(Vector3)`, which the HUD turns into a Box flying to the
goal counter.

- Subscribe in `OnEnable`, unsubscribe in `OnDisable` — not `Start`/`OnDestroy`, or an object that is
  disabled and re-enabled subscribes twice.
- Named methods, never lambdas: a lambda cannot be unsubscribed.
- `BlastResult` is a single reused instance. Read it during the call and never store it.

---

## Scope

**In:** blasting and icon tiers, the Box obstacle, gravity with segments, deadlock detection and
resolution, the objective and move limit, scoring, pooling, the sprite atlas, blast and landing
effects, 65 unit tests — and since v2, a home screen, a twelve-level campaign with coins and saved
progress, the level state machine, particles, sound and music.

**Out:** special blocks, boosters, chained combos, lives, a level editor, cloud save, localisation.
The `Scope and future work` section of `README.md` records the threshold at which each would start
paying for itself.

A board with no Boxes has no objective and no move limit, which is the shape both of the case
document's examples have. That is data rather than a second mode — the turn runs the same either way.
