using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NKG
{
    // Token: 0x02000196 RID: 406
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(tk2dSpriteAnimator))]
    public class FakeBatFix : MonoBehaviour
    {
        // Token: 0x06000908 RID: 2312 RVA: 0x00032508 File Offset: 0x00030708
        protected void Awake()
        {
            this.body = base.GetComponent<Rigidbody2D>();
            this.meshRenderer = base.GetComponent<MeshRenderer>();
            this.spriteAnimator = base.GetComponent<tk2dSpriteAnimator>();
            this.state = FakeBatFix.States.WaitingForBossAwake;
        }

        // Token: 0x06000909 RID: 2313 RVA: 0x00032535 File Offset: 0x00030735
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]

        // Token: 0x0600090A RID: 2314 RVA: 0x00032541 File Offset: 0x00030741
        protected void OnEnable()
        {
            FakeBatFix.fakeBats.Add(this);
        }

        // Token: 0x0600090B RID: 2315 RVA: 0x0003254E File Offset: 0x0003074E
        protected void OnDisable()
        {
            FakeBatFix.fakeBats.Remove(this);
        }

        // Token: 0x0600090C RID: 2316 RVA: 0x0003255C File Offset: 0x0003075C
        protected void Start()
        {
            float num = UnityEngine.Random.Range(0.7f, 0.9f);
            base.transform.SetScaleX(num);
            base.transform.SetScaleY(num);
            base.transform.SetPositionZ(0f);
        }

        // Token: 0x0600090D RID: 2317 RVA: 0x000325A1 File Offset: 0x000307A1
        protected void Update()
        {
            this.turnCooldown -= Time.deltaTime;
        }

        // Token: 0x0600090E RID: 2318 RVA: 0x000325B8 File Offset: 0x000307B8
        public static void NotifyAllBossAwake()
        {
            foreach (FakeBatFix fakeBat in FakeBatFix.fakeBats)
            {
                if (!(fakeBat == null))
                {
                    fakeBat.NotifyBossAwake();
                }
            }
        }

        // Token: 0x0600090F RID: 2319 RVA: 0x00032614 File Offset: 0x00030814
        public void NotifyBossAwake()
        {
            this.state = FakeBatFix.States.Dormant;
        }

        // Token: 0x06000910 RID: 2320 RVA: 0x00032620 File Offset: 0x00030820
        public static void SendAllOut()
        {
            foreach (FakeBatFix fakeBat in FakeBatFix.fakeBats)
            {
                if (!(fakeBat == null))
                {
                    fakeBat.SendOut();
                }
            }
        }

        // Token: 0x06000911 RID: 2321 RVA: 0x0003267C File Offset: 0x0003087C
        public void SendOut()
        {
            if (this.state != FakeBatFix.States.Dormant)
            {
                return;
            }
            base.StartCoroutine("SendOutRoutine");
            base.StopCoroutine("BringInRoutine");
        }

        // Token: 0x06000912 RID: 2322 RVA: 0x0003269F File Offset: 0x0003089F
        protected IEnumerator SendOutRoutine()
        {
            this.state = FakeBatFix.States.Out;
            this.transform.SetPosition2D(this.grimm.transform.position);
            this.transform.SetPositionZ(0f);
            this.meshRenderer.enabled = true;
            this.spriteAnimator.Play("Bat Fly");
            int? selectedDirection = null;
            for (; ; )
            {
                int num = selectedDirection ?? UnityEngine.Random.Range(0, 4);
                float minInclusive;
                float maxInclusive;
                float minInclusive2;
                float maxInclusive2;
                int num2;
                float num3;
                if (num == 1)
                {
                    minInclusive = 1f;
                    maxInclusive = 4f;
                    minInclusive2 = 2f;
                    maxInclusive2 = 3f;
                    num2 = 1;
                    num3 = 0.3f;
                }
                else if (num == 3)
                {
                    minInclusive = 1f;
                    maxInclusive = 4f;
                    minInclusive2 = -3f;
                    maxInclusive2 = -2f;
                    num2 = 1;
                    num3 = 0.3f;
                }
                else if (num == 2)
                {
                    minInclusive = -5f;
                    maxInclusive = -3f;
                    minInclusive2 = 0.5f;
                    maxInclusive2 = 2f;
                    num2 = 0;
                    num3 = 0.5f;
                }
                else
                {
                    minInclusive = 3f;
                    maxInclusive = 5f;
                    minInclusive2 = 0.5f;
                    maxInclusive2 = 2f;
                    num2 = 0;
                    num3 = 0.5f;
                }
                int index = (num2 + 1) % 2;
                Vector2 accel = new Vector2(UnityEngine.Random.Range(minInclusive, maxInclusive), UnityEngine.Random.Range(minInclusive2, maxInclusive2));
                if (UnityEngine.Random.Range(0, 1) == 0)
                {
                    accel[index] = -accel[index];
                }
                Vector2 velocity = this.body.velocity;
                ref Vector2 ptr = ref velocity;
                int index2 = num2;
                ptr[index2] *= num3;
                this.body.velocity = velocity;
                accel *= 0.5f;
                Vector2 maxSpeed = accel * 10f;
                maxSpeed.x = Mathf.Abs(maxSpeed.x);
                maxSpeed.y = Mathf.Abs(maxSpeed.y);
                float timer;
                for (timer = 0.2f; timer > 0f; timer -= Time.deltaTime)
                {
                    this.FaceDirection((this.body.velocity.x > 0f) ? 1 : -1, false);
                    this.Accelerate(accel, new Vector2(15f, 10f));
                    yield return null;
                }
                selectedDirection = null;
                timer = UnityEngine.Random.Range(0.5f, 1.5f);
                while (selectedDirection == null && timer > 0f)
                {
                    this.FaceDirection((this.body.velocity.x > 0f) ? 1 : -1, false);
                    this.Accelerate(accel, maxSpeed);
                    Vector2 vector = this.transform.position;
                    if (vector.x < LeftLimit)
                    {
                        selectedDirection = new int?(0);
                        break;
                    }
                    if (vector.x > RightLimit)
                    {
                        selectedDirection = new int?(2);
                        break;
                    }
                    if (vector.y < DownLimit)
                    {
                        selectedDirection = new int?(1);
                        break;
                    }
                    if (vector.y > UpLimit)
                    {
                        selectedDirection = new int?(3);
                        break;
                    }
                    yield return null;
                    timer -= Time.deltaTime;
                }
                accel = default(Vector2);
                maxSpeed = default(Vector2);
            }
        }

        // Token: 0x06000913 RID: 2323 RVA: 0x000326B0 File Offset: 0x000308B0
        public static void BringAllIn()
        {
            foreach (FakeBatFix fakeBat in FakeBatFix.fakeBats)
            {
                if (!(fakeBat == null))
                {
                    fakeBat.BringIn();
                }
            }
        }

        // Token: 0x06000914 RID: 2324 RVA: 0x0003270C File Offset: 0x0003090C
        public void BringIn()
        {
            base.StartCoroutine("BringInRoutine");
            base.StopCoroutine("SendOutRoutine");
        }

        // Token: 0x06000915 RID: 2325 RVA: 0x00032725 File Offset: 0x00030925
        protected IEnumerator BringInRoutine()
        {
            this.state = FakeBatFix.States.In;
            int sign = (this.grimm.transform.position.x - this.body.velocity.x > 0f) ? 1 : -1;
            this.FaceDirection(sign, true);
            this.body.velocity = Vector2.zero;
            for (; ; )
            {
                Vector2 current = this.transform.position;
                Vector2 vector = this.grimm.transform.position;
                Vector2 vector2 = Vector2.MoveTowards(current, vector, 25f * Time.deltaTime);
                this.transform.SetPosition2D(vector2);
                if (Vector2.Distance(vector2, vector) < Mathf.Epsilon)
                {
                    break;
                }
                yield return null;
            }
            this.spriteAnimator.Play("Bat End");
            while (this.spriteAnimator.ClipTimeSeconds < this.spriteAnimator.CurrentClip.Duration - Mathf.Epsilon)
            {
                yield return null;
            }
            this.meshRenderer.enabled = false;
            this.transform.SetPositionY(-50f);
            this.state = FakeBatFix.States.Dormant;
            yield break;
        }

        // Token: 0x06000916 RID: 2326 RVA: 0x00032734 File Offset: 0x00030934
        private void FaceDirection(int sign, bool snap)
        {
            float num = Mathf.Abs(base.transform.localScale.x) * (float)sign;
            if (!Mathf.Approximately(base.transform.localScale.x, num) && (snap || this.turnCooldown <= 0f))
            {
                if (!snap)
                {
                    this.spriteAnimator.Play("Bat TurnToFly");
                    this.spriteAnimator.PlayFromFrame(0);
                    this.turnCooldown = 0.5f;
                }
                base.transform.SetScaleX(num);
            }
        }

        // Token: 0x06000917 RID: 2327 RVA: 0x000327B8 File Offset: 0x000309B8
        private void Accelerate(Vector2 fixedAcceleration, Vector2 speedLimit)
        {
            Vector2 a = fixedAcceleration / Time.fixedDeltaTime;
            Vector2 vector = this.body.velocity;
            vector += a * Time.deltaTime;
            vector.x = Mathf.Clamp(vector.x, -speedLimit.x, speedLimit.x);
            vector.y = Mathf.Clamp(vector.y, -speedLimit.y, speedLimit.y);
            this.body.velocity = vector;
        }

        // Token: 0x04000A20 RID: 2592
        private Rigidbody2D body;

        // Token: 0x04000A21 RID: 2593
        private MeshRenderer meshRenderer;

        // Token: 0x04000A22 RID: 2594
        private tk2dSpriteAnimator spriteAnimator;

        // Token: 0x04000A23 RID: 2595
        private FakeBatFix.States state;

        // Token: 0x04000A24 RID: 2596
        private float turnCooldown;

        // Token: 0x04000A25 RID: 2597
        [SerializeField]
        private Transform grimm;

        // Token: 0x04000A26 RID: 2598
        private const float Z = 0f;

        // Token: 0x04000A27 RID: 2599
        private static List<FakeBatFix> fakeBats = new List<FakeBatFix>();

        // Token: 0x02000197 RID: 407
        private enum States
        {
            // Token: 0x04000A29 RID: 2601
            WaitingForBossAwake,
            // Token: 0x04000A2A RID: 2602
            Dormant,
            // Token: 0x04000A2B RID: 2603
            In,
            // Token: 0x04000A2C RID: 2604
            Out
        }

        public float UpLimit, DownLimit, LeftLimit, RightLimit;
    }

}