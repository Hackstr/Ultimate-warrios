using UnityEngine;

namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// Factory for creating particle system VFX programmatically.
    /// Replaces procedural primitive-based VFX with proper particle effects.
    /// </summary>
    public static class ParticleVFXFactory
    {
        private static Material _particleMaterial;

        private static Material GetParticleMaterial()
        {
            if (_particleMaterial != null) return _particleMaterial;
            var shader = Shader.Find("Particles/Standard Unlit")
                      ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                      ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogWarning("[ParticleVFXFactory] No particle shader found, using fallback");
                _particleMaterial = new Material(Shader.Find("Standard") ?? Shader.Find("Hidden/InternalErrorShader"));
                return _particleMaterial;
            }
            _particleMaterial = new Material(shader);
            _particleMaterial.SetFloat("_Mode", 2); // Fade
            return _particleMaterial;
        }

        /// <summary>
        /// Radial burst of sparks — used for hit, armor break, mutual cancel.
        /// </summary>
        public static GameObject CreateBurst(Color color, float size = 0.4f, int count = 20, float lifetime = 0.6f)
        {
            var go = new GameObject("VFX_Burst");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.3f, size * 0.8f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count;
            main.gravityModifier = 0.5f;
            main.loop = false;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = size * 0.2f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = CreateFadeGradient(color);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            SetupRenderer(go, color);
            return go;
        }

        /// <summary>
        /// Expanding ring — used for push shockwave, scan pulse.
        /// </summary>
        public static GameObject CreateExpandingRing(Color color, float maxRadius = 1.5f, float lifetime = 0.8f)
        {
            var go = new GameObject("VFX_Ring");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;
            main.startSize = 0.06f;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            main.loop = false;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;
            shape.radiusThickness = 0f; // edge only

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.radial = maxRadius / lifetime;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = CreateFadeGradient(color);

            SetupRenderer(go, color);
            return go;
        }

        /// <summary>
        /// Muzzle flash — brief bright burst at gun barrel.
        /// </summary>
        public static GameObject CreateMuzzleFlash(Color color)
        {
            var go = new GameObject("VFX_MuzzleFlash");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.15f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = Color.Lerp(color, Color.white, 0.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 12;
            main.loop = false;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.05f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            SetupRenderer(go, color);
            return go;
        }

        /// <summary>
        /// Large dramatic explosion burst — used for eliminations.
        /// </summary>
        public static GameObject CreateEliminationBurst(Color color)
        {
            var go = new GameObject("VFX_Elimination");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            main.gravityModifier = 1.2f;
            main.loop = false;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] {
                new ParticleSystem.Burst(0f, 25),
                new ParticleSystem.Burst(0.05f, 15),
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(Color.Lerp(color, Color.white, 0.6f), 0f),
                    new GradientColorKey(color, 0.3f),
                    new GradientColorKey(color * 0.5f, 1f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.EaseInOut(0, 1, 1, 0.2f));

            // Add secondary flash
            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(go.transform, false);
            var flashPs = flashGo.AddComponent<ParticleSystem>();
            var flashMain = flashPs.main;
            flashMain.startLifetime = 0.2f;
            flashMain.startSpeed = 0f;
            flashMain.startSize = 1.5f;
            flashMain.startColor = new Color(1f, 0.9f, 0.7f, 0.8f);
            flashMain.loop = false;
            flashMain.playOnAwake = true;

            var flashEmission = flashPs.emission;
            flashEmission.rateOverTime = 0;
            flashEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var flashSize = flashPs.sizeOverLifetime;
            flashSize.enabled = true;
            flashSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 3f));

            var flashColor = flashPs.colorOverLifetime;
            flashColor.enabled = true;
            flashColor.color = CreateFadeGradient(new Color(1f, 0.9f, 0.7f, 0.8f));

            SetupRenderer(go, color);
            SetupRenderer(flashGo, new Color(1f, 0.9f, 0.7f));

            return go;
        }

        /// <summary>
        /// Trailing particles along a direction — used for projectiles.
        /// </summary>
        public static GameObject CreateProjectileTrail(Color color, float speed = 15f)
        {
            var go = new GameObject("VFX_Projectile");
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.3f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;
            main.loop = true;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 80;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.03f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = CreateFadeGradient(color);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

            // Core bright particle (single, large)
            var coreGo = new GameObject("Core");
            coreGo.transform.SetParent(go.transform, false);
            var corePs = coreGo.AddComponent<ParticleSystem>();
            var coreMain = corePs.main;
            coreMain.startLifetime = 999f;
            coreMain.startSpeed = 0f;
            coreMain.startSize = 0.2f;
            coreMain.startColor = Color.Lerp(color, Color.white, 0.7f);
            coreMain.loop = false;
            coreMain.playOnAwake = true;

            var coreEmission = corePs.emission;
            coreEmission.rateOverTime = 0;
            coreEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            SetupRenderer(go, color);
            SetupRenderer(coreGo, Color.Lerp(color, Color.white, 0.7f));

            // Add mover
            go.AddComponent<SimpleMover>().speed = speed;

            return go;
        }

        // ── Helpers ──

        private static Gradient CreateFadeGradient(Color color)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(0f, 1f) }
            );
            return gradient;
        }

        private static void SetupRenderer(GameObject go, Color color)
        {
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer == null) return;
            var mat = new Material(GetParticleMaterial());
            mat.color = color;
            renderer.material = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
    }
}
