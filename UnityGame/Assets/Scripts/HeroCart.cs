using System.Collections.Generic;
using UnityEngine;

namespace RollerCoasterHeroes
{
    public sealed class HeroCart
    {
        public Transform Root { get; }
        readonly Transform body, hero, head, leftArm, rightArm, tail, scarf;
        readonly List<Transform> wheels = new List<Transform>();
        readonly List<Transform> eyes = new List<Transform>();
        float wheelAngle, celebration;

        public HeroCart(RideArt art)
        {
            Root = art.Group("FOX - hero cart");
            body = art.Group("Suspended body", Root);
            art.Part("Chassis", PrimitiveType.Cube, body, new Vector3(0, .49f, 0), new Vector3(1.6f, .28f, 2.3f), RideArt.Navy);
            art.Part("Rounded coral shell", PrimitiveType.Capsule, body, new Vector3(0, .95f, 0), new Vector3(1.8f, .65f, 2.5f), RideArt.Coral);
            art.Part("Seat cushion", PrimitiveType.Cube, body, new Vector3(0, 1.28f, -.23f), new Vector3(1.18f, .16f, 1.15f), RideArt.Navy);
            art.Part("Padded seat back", PrimitiveType.Cube, body, new Vector3(0, 1.7f, -.7f), new Vector3(1.3f, 1, .27f), RideArt.Navy);
            art.Part("Front nose", PrimitiveType.Sphere, body, new Vector3(0, 1.13f, .9f), new Vector3(1.65f, .65f, .8f), RideArt.Coral);
            art.Part("Gold racing stripe", PrimitiveType.Cube, body, new Vector3(0, 1.45f, .98f), new Vector3(.23f, .035f, .65f), RideArt.Gold);
            art.Part("Safety lap bar", PrimitiveType.Cylinder, body, new Vector3(0, 1.65f, .39f), new Vector3(.09f, .72f, .09f), RideArt.Teal).localRotation = Quaternion.Euler(0, 0, 90);
            for (int side = -1; side <= 1; side += 2)
            {
                art.Part("Side gold trim", PrimitiveType.Cube, body, new Vector3(side * .87f, 1.13f, 0), new Vector3(.06f, .12f, 1.7f), RideArt.Gold);
                art.Part("Headlight", PrimitiveType.Sphere, body, new Vector3(side * .54f, 1.12f, 1.24f), new Vector3(.27f, .22f, .10f), new Color(1, .96f, .71f));
                for (int axle = -1; axle <= 1; axle += 2)
                {
                    var pivot = art.Group("Rolling wheel", Root); pivot.localPosition = new Vector3(side * .72f, .34f, axle * .76f);
                    var tire = art.Part("Rubber tire", PrimitiveType.Cylinder, pivot, Vector3.zero, new Vector3(.58f, .13f, .58f), RideArt.Navy);
                    tire.localRotation = Quaternion.Euler(0, 0, 90);
                    var hub = art.Part("Gold hub", PrimitiveType.Cylinder, pivot, new Vector3(side * .14f, 0, 0), new Vector3(.32f, .025f, .32f), RideArt.Gold);
                    hub.localRotation = Quaternion.Euler(0, 0, 90);
                    art.Part("Hub spoke", PrimitiveType.Cube, pivot, new Vector3(side * .17f, 0, 0), new Vector3(.04f, .05f, .35f), RideArt.Teal);
                    wheels.Add(pivot);
                }
            }
            hero = art.Group("FOX animated rig", body);
            hero.localPosition = new Vector3(0, 1.27f, -.15f);
            Color orange = new Color(1, .46f, .08f), cream = new Color(1, .91f, .72f);
            art.Part("Jacket", PrimitiveType.Capsule, hero, new Vector3(0, .42f, 0), new Vector3(.75f, .65f, .65f), RideArt.Teal);
            art.Part("Cream chest", PrimitiveType.Sphere, hero, new Vector3(0, .5f, .29f), new Vector3(.40f, .5f, .12f), cream);
            head = art.Group("Head pivot", hero); head.localPosition = new Vector3(0, 1.0f, .02f);
            art.Part("Fox head", PrimitiveType.Sphere, head, Vector3.zero, new Vector3(1, .9f, .85f), orange);
            for (int side = -1; side <= 1; side += 2)
            {
                art.Part("Pointed ear", PrimitiveType.Capsule, head, new Vector3(side * .34f, .46f, -.05f), new Vector3(.26f, .35f, .27f), orange).localRotation = Quaternion.Euler(0, 0, -side * 22);
                art.Part("Ear cream", PrimitiveType.Sphere, head, new Vector3(side * .34f, .48f, .06f), new Vector3(.13f, .26f, .08f), cream);
                art.Part("Cheek", PrimitiveType.Sphere, head, new Vector3(side * .23f, -.17f, .34f), new Vector3(.49f, .36f, .32f), cream);
                var eye = art.Part("Blinking eye", PrimitiveType.Sphere, head, new Vector3(side * .23f, .10f, .37f), new Vector3(.14f, .21f, .08f), RideArt.Navy); eyes.Add(eye);
                art.Part("Eye sparkle", PrimitiveType.Sphere, eye, new Vector3(-.12f, .2f, .47f), new Vector3(.28f, .25f, .25f), Color.white);
            }
            art.Part("Nose", PrimitiveType.Sphere, head, new Vector3(0, -.12f, .53f), new Vector3(.22f, .16f, .17f), RideArt.Navy);
            art.Part("Smile", PrimitiveType.Sphere, head, new Vector3(0, -.29f, .39f), new Vector3(.21f, .07f, .06f), RideArt.Navy);
            leftArm = Arm(art, hero, -1, orange); rightArm = Arm(art, hero, 1, orange);
            tail = art.Group("Tail pivot", hero); tail.localPosition = new Vector3(0, .12f, -.38f);
            art.Part("Fluffy tail", PrimitiveType.Capsule, tail, new Vector3(.2f, .12f, -.4f), new Vector3(.4f, .6f, .45f), orange).localRotation = Quaternion.Euler(65, 0, -25);
            art.Part("Cream tail tip", PrimitiveType.Sphere, tail, new Vector3(.23f, .25f, -.7f), new Vector3(.33f, .33f, .35f), cream);
            scarf = art.Part("Flying scarf", PrimitiveType.Cube, hero, new Vector3(.35f, .83f, -.48f), new Vector3(.25f, .10f, .85f), RideArt.Gold);
        }

        static Transform Arm(RideArt art, Transform hero, int side, Color orange)
        {
            var arm = art.Group("Arm pivot", hero); arm.localPosition = new Vector3(side * .4f, .65f, 0);
            art.Part("Sleeve", PrimitiveType.Capsule, arm, new Vector3(0, -.19f, 0), new Vector3(.22f, .22f, .23f), RideArt.Teal);
            art.Part("Paw", PrimitiveType.Sphere, arm, new Vector3(0, -.43f, 0), Vector3.one * .26f, orange);
            return arm;
        }

        public void React() { celebration = 1; }
        public void Reset() { wheelAngle = celebration = 0; }
        public void Animate(float dt, float speed, bool finished, PickupKind effect, float effectSeconds, float time)
        {
            wheelAngle = (wheelAngle + speed / .29f * Mathf.Rad2Deg * dt) % 360;
            foreach (var w in wheels) w.localRotation = Quaternion.Euler(wheelAngle, 0, 0);
            celebration = Mathf.Max(0, celebration - dt * 1.5f);
            float wave = Mathf.Sin(time * 7);
            body.localPosition = new Vector3(0, Mathf.Sin(time * 12) * .018f * speed / 20, 0);
            hero.localRotation = Quaternion.Euler(0, 0, wave * (finished ? 5 : 2));
            head.localRotation = Quaternion.Euler(Mathf.Sin(time * 3) * 4, Mathf.Sin(time * 1.8f) * 9, 0);
            bool cheer = finished || celebration > 0 || (effectSeconds > 0 && effect == PickupKind.Boost);
            float raised = cheer ? 150 : effectSeconds > 0 && effect == PickupKind.Brake ? 75 : 55;
            leftArm.localRotation = Quaternion.Euler(-raised, 0, cheer ? -25 - wave * 16 : -12);
            rightArm.localRotation = Quaternion.Euler(-raised, 0, cheer ? 25 + wave * 16 : 12);
            tail.localRotation = Quaternion.Euler(0, wave * 18, 0);
            scarf.localRotation = Quaternion.Euler(wave * 10, wave * 12, 12);
            float blink = time % 4.3f < .13f ? .025f : .21f;
            foreach (var eye in eyes) eye.localScale = new Vector3(.14f, blink, .08f);
        }
    }
}
