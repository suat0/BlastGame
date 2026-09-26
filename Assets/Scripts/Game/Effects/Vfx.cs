using UnityEngine;

namespace BlastGame.Game
{
    // The game's particles: one ParticleSystem per kind, each emitting on demand at a position.
    //
    // Emit rather than a pool of prefab instances: a system that emits wherever it is told needs no
    // instance per effect, no pool and no return, and EmitParams is a struct, so a burst allocates
    // nothing. All of them share one material (see ParticlePremultiplied.shader), which is why the
    // whole of this costs one draw call on top of the board's.
    //
    // Cosmetic like everything in Effects: a burst that does not happen is a missing sparkle.
    public sealed class Vfx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem sparkles;
        [SerializeField] private ParticleSystem glows;
        [SerializeField] private ParticleSystem splinters;
        [SerializeField] private ParticleSystem dust;
        [SerializeField] private ParticleSystem confetti;

        private ParticleSystem.EmitParams at;

        private void Awake()
        {
            // Every burst starts from the system's own shape around this position.
            at.applyShapeToPosition = true;
        }

        public void Sparkles(Vector3 position, int count) => Emit(sparkles, position, count);

        // One soft flash behind a blast, sized by how big the group was.
        public void Glow(Vector3 position, float size, Color color)
        {
            at.position = position;
            at.startSize = size;
            at.startColor = color;

            glows.Emit(at, 1);

            at.ResetStartSize();
            at.ResetStartColor();
        }

        public void Splinters(Vector3 position, int count) => Emit(splinters, position, count);

        public void Dust(Vector3 position, int count) => Emit(dust, position, count);

        public void Confetti(Vector3 position, int count) => Emit(confetti, position, count);

        private void Emit(ParticleSystem system, Vector3 position, int count)
        {
            at.position = position;
            system.Emit(at, count);
        }
    }
}
