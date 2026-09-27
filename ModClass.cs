using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using InControl;
using Modding;
using On;
using Satchel;
using Satchel.Futils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Reflection;
using UnityEngine;
using static UnityEngine.UI.Image;

namespace NKG
{
    public class NKG : Mod
    {
        public NKG() : base("NKG Anywhere") { }
        public override string GetVersion() => "0.1";
        public override void Initialize()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
            ModHooks.HeroUpdateHook += ModHooks_HeroUpdateHook;
            On.HeroController.OnCollisionEnter2D += GroundDetect;
        }

        private static void GroundDetect(On.HeroController.orig_OnCollisionEnter2D orig, HeroController self, Collision2D collision)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if(contact.normal.y > 0.5f) Yvalue = contact.point.y;
                //Log("Y level changed to " + Yvalue);
                //DrawLine(Ylevel);
            }
            orig(self, collision);
        }

        public static GameObject Nkg = null, Spikes = null, Bats = null;
        public static PlayMakerFSM batfsm = null;
        public static float Yvalue = 0;

        private void SceneManager_activeSceneChanged(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
        {
            if (arg1.name.Contains("Nightmare"))
            {
                IEnumerator Night()
                {
                    yield return new WaitForFinishedEnteringScene();

                    Nkg = GameObject.Instantiate(GameObject.Find("Nightmare Grimm Boss"));
                    GameObject.DontDestroyOnLoad(Nkg);
                    Nkg.SetActive(false);

                    Spikes = GameObject.Instantiate(GameObject.Find("Grimm Spike Holder"));
                    GameObject.DontDestroyOnLoad(Spikes);
                    Spikes.SetActive(false);

                    Bats = GameObject.Instantiate(GameObject.Find("Grimm Bats"));
                    GameObject.DontDestroyOnLoad(Bats);
                    Bats.SetActive(false);
                }
                GameManager.instance.StartCoroutine(Night());
            }
        }

        public class BatLimitSetter : MonoBehaviour
        {
            /*public void Awake()
            {
                FakeBatFix bat = gameObject.GetComponent<FakeBatFix>();
                FakeBatFix.fakeBats.Add(bat);
            }*/
            public void FixedUpdate()
            {
                PlayMakerFSM batfsm = GameObject.Find("Real Bat").LocateMyFSM("Control");
                FakeBatFix bat = gameObject.GetComponent<FakeBatFix>();
                bat.LeftLimit = batfsm.GetAction<FloatCompare>("Fly", 3).float2.Value;
                bat.RightLimit = batfsm.GetAction<FloatCompare>("Fly", 4).float2.Value;
                bat.DownLimit = batfsm.GetAction<FloatCompare>("Fly", 5).float2.Value;
                bat.UpLimit = batfsm.GetAction<FloatCompare>("Fly", 6).float2.Value;
            }

        }


        public class NightmareKingGrimm : MonoBehaviour
        {
            GameObject spikes = null, bats = null;
            public void Start()
            {
                spikes = GameObject.Instantiate(NKG.Spikes);
                spikes.SetActive(true);
                bats = GameObject.Instantiate(NKG.Bats);
                bats.SetActive(true);
                Modding.Logger.Log("Waking up");
                gameObject.LocateMyFSM("Control").SendEvent("WAKE");
                batfsm = GameObject.Find("Real Bat").LocateMyFSM("Control");

                GameObject NkgArchi = new GameObject("Nightmare Grimm Boss Archi");
                gameObject.transform.SetParent(NkgArchi.transform, true);
                spikes.transform.SetParent(NkgArchi.transform, true);
                bats.transform.SetParent(NkgArchi.transform, true);

                PlayMakerFSM control = gameObject.LocateMyFSM("Control");
                control.InsertCustomAction("Pillar", () =>
                {
                    float poin = FindGround(GameObject.Find("Knight").transform.position.x, Yvalue);
                    //Modding.Logger.Log(poin);
                    control.GetFirstActionOfType<SetPosition>("Pillar").y.Value = poin - 0.2f;
                }, 0);

                control.InsertCustomAction("Pillar Tele In", () =>
                {
                    float Ylevel = Yvalue + 3;
                    GameObject[] arr = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    foreach (GameObject objj in arr)
                    {
                        if (8 == objj.layer)
                        {
                            Collider2D[] colliders = objj.GetComponentsInChildren<Collider2D>(true);

                            foreach (Collider2D collider in colliders)
                            {
                                float Xpos = control.FsmVariables.GetFsmFloat("Tele X").Value;
                                if (collider.OverlapPoint(new Vector2(Xpos + 0.6f, Ylevel + 8.5f)) && collider.OverlapPoint(new Vector2(Xpos + 0.6f, Ylevel + 5.9f)) &&
                                    collider.OverlapPoint(new Vector2(Xpos - 0.6f, Ylevel + 8.5f)) && collider.OverlapPoint(new Vector2(Xpos - 0.6f, Ylevel + 5.9f)))
                                {
                                    Modding.Logger.Log("Found teleport inside of block for Pillar");
                                    control.SetState("Move Choice");
                                }
                            }
                        }
                    }
                    Vector2 RaycastPos = new Vector2(control.FsmVariables.GetFsmFloat("Tele X").Value, Yvalue + 5.3f);
                    if (KnightVision(RaycastPos, control.ActiveStateName)) control.SetState("Move Choice");
                }, 0);

                control.ChangeTransition("Death Start", "FINISHED", "Steam");

                control.GetValidState("Spike Attack").Actions[0] = new CustomFsmAction
                {
                    method = () =>
                    {
                        float Xlevel = GameObject.Find("Knight").transform.position.x;
                        float Ylevel = Yvalue + 3;
                        PlayMakerFSM fsm = spikes.LocateMyFSM("Spike Control");
                        //fsm.GetFirstActionOfType<RandomFloatEither>("Ready").value1 = Xlevel + 0.5625f;
                        //fsm.GetFirstActionOfType<RandomFloatEither>("Ready").value1 = Xlevel - 0.5625f;
                        fsm.GetFirstActionOfType<SetPosition>("Ready").x = UnityEngine.Random.Range(Xlevel + 0.5625f, Xlevel - 0.5625f) - 20;
                        //fsm.GetFirstActionOfType<SetPosition>("Ready").y = Ylevel - 4.68f;
                        fsm.SendEvent("SPIKE ATTACK");
                    }
                };

                GameObject[] arra = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach(GameObject obj in arra)
                {
                    if (obj.name.Contains("Nightmare Spike"))
                    {
                        PlayMakerFSM fsm = obj.LocateMyFSM("Control");
                        fsm.GetValidState("GG Y Shift").Actions[0] = new CustomFsmAction
                        {
                            method = () =>
                            {
                                float poin = FindGround(obj.transform.position.x, Yvalue);
                                fsm.GetFirstActionOfType<SetPosition>("GG Y Shift").y.Value = poin - 4.18f;
                            }
                        };
                        fsm.GetValidState("GG Y Shift").Actions[3] = new CustomFsmAction
                        {
                            method = () =>
                            {
                                fsm.SendEvent("FINISHED");
                            }
                        };
                    }
                }

                control.InsertCustomAction("AD Tele In", () =>
                {
                    float Ylevel = Yvalue + 3;
                    GameObject[] arr = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    foreach (GameObject objj in arr)
                    {
                        if (8 == objj.layer)
                        {
                            Collider2D[] colliders = objj.GetComponentsInChildren<Collider2D>(true);

                            foreach (Collider2D collider in colliders)
                            {
                                float Xpos = control.FsmVariables.GetFsmFloat("Tele X").Value;
                                if (collider.OverlapPoint(new Vector2(Xpos + 1.2f, Ylevel + 8.5f)) || collider.OverlapPoint(new Vector2(Xpos + 1.2f, Ylevel + 5.9f)) ||
                                    collider.OverlapPoint(new Vector2(Xpos - 1.2f, Ylevel + 8.5f)) || collider.OverlapPoint(new Vector2(Xpos - 1.2f, Ylevel + 5.9f)))
                                {
                                    Modding.Logger.Log("Found teleport inside of block for Dive");
                                    control.SetState("Move Choice");
                                }
                            }
                        }
                    }
                    Vector2 RaycastPos = new Vector2(control.FsmVariables.GetFsmFloat("Tele X").Value, Yvalue + 6.7f);
                    if (KnightVision(RaycastPos, control.ActiveStateName)) control.SetState("Move Choice");
                }, 0);

                control.AddCustomAction("AD Fire", () =>
                {
                    IEnumerator Stopper()
                    {
                        while (true)
                        {
                            yield return null;
                            float ground = FindGround(gameObject.transform.position.x, gameObject.transform.position.y);
                            if (ground >= gameObject.transform.position.y - 3 || gameObject.transform.position.y < Yvalue)
                            {
                                Modding.Logger.Log("Found Ground to collide with and ending dash");
                                control.SendEvent("LAND");
                                break;
                            }
                        }
                    }

                    GameManager.instance.StartCoroutine(Stopper());
                });

                control.RemoveAction("FB Hero Pos", 1);
                control.AddCustomAction("FB Hero Pos", () =>
                {
                    if (UnityEngine.Random.Range(-1, 1) > 0)
                    {
                        control.SendEvent("HERO L");
                    }
                    else control.SendEvent("HERO R");
                });

                control.InsertCustomAction("FB Tele L", () =>
                {
                    float Ylevel = Yvalue + 3;
                    GameObject[] arr = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    foreach (GameObject objj in arr)
                    {
                        if (8 == objj.layer)
                        {
                            Collider2D[] colliders = objj.GetComponentsInChildren<Collider2D>(true);

                            foreach (Collider2D collider in colliders)
                            {
                                float Xpos = control.FsmVariables.GetFsmFloat("Tele X").Value;
                                if (collider.OverlapPoint(new Vector2(Xpos + 0.6f, Ylevel + 8.5f)) && collider.OverlapPoint(new Vector2(Xpos + 0.6f, Ylevel + 5.9f)) &&
                                    collider.OverlapPoint(new Vector2(Xpos - 0.6f, Ylevel + 8.5f)) && collider.OverlapPoint(new Vector2(Xpos - 0.6f, Ylevel + 5.9f)))
                                {
                                    Modding.Logger.Log("Found teleport inside of block for L Bats");
                                    control.SetState("Move Choice");
                                }
                            }
                        }
                    }
                    Vector2 RaycastPos = new Vector2(control.FsmVariables.GetFsmFloat("Tele X").Value, Yvalue + 1);
                    if (KnightVision(RaycastPos, control.ActiveStateName)) control.SetState("Move Choice");
                }, 1);

                control.InsertCustomAction("FB Tele R", () =>
                {
                    float Ylevel = Yvalue + 3;
                    GameObject[] arr = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    foreach (GameObject objj in arr)
                    {
                        if (8 == objj.layer)
                        {
                            Collider2D[] colliders = objj.GetComponentsInChildren<Collider2D>(true);

                            foreach (Collider2D collider in colliders)
                            {
                                float Xpos = control.FsmVariables.GetFsmFloat("Tele X").Value;
                                if (collider.OverlapPoint(new Vector2(Xpos + 0.6f, Ylevel + 8.5f)) && collider.OverlapPoint(new Vector2(Xpos + 0.6f, Ylevel + 5.9f)) &&
                                    collider.OverlapPoint(new Vector2(Xpos - 0.6f, Ylevel + 8.5f)) && collider.OverlapPoint(new Vector2(Xpos - 0.6f, Ylevel + 5.9f)))
                                {
                                    Modding.Logger.Log("Found teleport inside of block for R Bats");
                                    control.SetState("Move Choice");
                                }
                            }
                        }
                    }
                    Vector2 RaycastPos = new Vector2(control.FsmVariables.GetFsmFloat("Tele X").Value, Yvalue + 1);
                    if (KnightVision(RaycastPos, control.ActiveStateName)) control.SetState("Move Choice");
                }, 1);

                control.InsertCustomAction("Slash Pos", () =>
                {
                    Collider2D enemyCollider = gameObject.GetComponent<Collider2D>();
                    GameObject[] arr = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    foreach(GameObject obj in arr)
                    {
                        if (8 == obj.layer)
                        {
                            Collider2D[] terrainColliders = obj.GetComponentsInChildren<Collider2D>(true);

                            foreach (Collider2D terrainCollider in terrainColliders)
                            {
                                Physics2D.IgnoreCollision(enemyCollider, terrainCollider, true);
                            }
                        }
                    }
                }, 0);

                control.InsertCustomAction("Slash Tele In", () =>
                {
                    float Ylevel = Yvalue + 3;
                    GameObject[] arr = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    foreach (GameObject objj in arr)
                    {
                        if (8 == objj.layer)
                        {
                            Collider2D[] colliders = objj.GetComponentsInChildren<Collider2D>(true);

                            foreach (Collider2D collider in colliders)
                            {
                                float Xpos = control.FsmVariables.GetFsmFloat("Tele X").Value;
                                if (collider.OverlapPoint(new Vector2(Xpos + 0.6f, Ylevel + 8.5f)) && collider.OverlapPoint(new Vector2(Xpos + 0.6f, Ylevel + 5.9f)) &&
                                    collider.OverlapPoint(new Vector2(Xpos - 0.6f, Ylevel + 8.5f)) && collider.OverlapPoint(new Vector2(Xpos - 0.6f, Ylevel + 5.9f)))
                                {
                                    Modding.Logger.Log("Found teleport inside of block for Slash");
                                    control.SetState("Move Choice");
                                }
                            }
                        }
                    }
                    Vector2 RaycastPos = new Vector2(control.FsmVariables.GetFsmFloat("Tele X").Value, Yvalue + 1);
                    if (KnightVision(RaycastPos, control.ActiveStateName)) control.SetState("Move Choice");
                }, 0);

                control.InsertCustomAction("Move Choice", () =>
                {
                    Collider2D enemyCollider = gameObject.GetComponent<Collider2D>();
                    GameObject[] arr = UnityEngine.Object.FindObjectsOfType<GameObject>();
                    foreach (GameObject obj in arr)
                    {
                        if (8 == obj.layer)
                        {
                            Collider2D[] terrainColliders = obj.GetComponentsInChildren<Collider2D>(true);

                            foreach (Collider2D terrainCollider in terrainColliders)
                            {
                                Physics2D.IgnoreCollision(enemyCollider, terrainCollider, false);
                            }
                        }
                    }
                }, 0);

                control.GetValidState("Explode").Actions[3] = new CustomFsmAction
                {
                    method = () =>
                    {
                        batfsm.SendEvent("BATS OUT");
                    }
                };

                control.GetValidState("Call Bats").Actions[0] = new CustomFsmAction
                {
                    method = () =>
                    {
                        batfsm.SendEvent("BATS IN");
                    }
                };

                batfsm.SendEvent("BOSS AWAKE");
                batfsm.FsmVariables.GetFsmGameObject("Grimm").Value = gameObject;

                GameObject[] arr = UnityEngine.Object.FindObjectsOfType<GameObject>();
                List<GameObject> BatList = new List<GameObject>();

                foreach(GameObject obj in arr)
                {
                    if(obj.name.Contains("Fake Bat"))
                    {
                        BatList.Add(obj);
                        UnityEngine.Object.Destroy(obj.GetComponent<FakeBat>());
                        obj.AddComponent<FakeBatFix>();
                        obj.AddComponent<BatLimitSetter>();
                        var fake = obj.GetComponent<FakeBatFix>();

                        FieldInfo field1 = typeof(FakeBatFix).GetField
                            (
                                "grimm",
                                BindingFlags.Instance | BindingFlags.NonPublic
                            );
                        field1.SetValue(fake, gameObject.transform);
                    }
                }

                Satchel.CoroutineHelper.WaitForSecondsBeforeInvoke(0.1f, () => FakeBatFix.NotifyAllBossAwake());

                control.GetValidState("Explode").Actions[4] = new CustomFsmAction
                {
                    method = () =>
                    {
                        foreach (GameObject obj in BatList)
                        {
                            obj.GetComponent<FakeBatFix>().SendOut();
                        }
                    }
                };
                control.GetValidState("Call Bats").Actions[1] = new CustomFsmAction
                {
                    method = () =>
                    {
                        foreach (GameObject obj in BatList)
                        {
                            obj.GetComponent<FakeBatFix>().BringIn();
                        }
                    }
                };

                UnityEngine.Object.Destroy(gameObject.LocateMyFSM("constrain_x"));

                UnityEngine.Object.Destroy(gameObject.LocateMyFSM("Constrain Y"));
            }

            public void FixedUpdate()
            {
                float Xlevel = GameObject.Find("Knight").transform.position.x;
                float Ylevel = Yvalue + 3;
                PlayMakerFSM control = gameObject.LocateMyFSM("Control");
                control.FsmVariables.GetFsmFloat("Ground Y").Value = Ylevel;
                control.FsmVariables.GetFsmFloat("Mid Y").Value = Ylevel + 3.7f;
                control.GetFirstActionOfType<SetPosition>("Balloon Pos").y.Value = Ylevel + 5.2f;
                control.GetFirstActionOfType<SetPosition>("UP Explode").y.Value = Ylevel + 9;
                control.GetFirstActionOfType<FloatCompare>("Uppercut Up").float2.Value = Ylevel + 9;
                control.GetFirstActionOfType<SetPosition>("AD Tele In").y.Value = Ylevel + 6.7f;
                control.GetFirstActionOfType<SetPosition>("Pillar Tele In").y.Value = Ylevel + 4.8f;

                control.FsmVariables.GetFsmFloat("AD Max X").Value = Xlevel + 12;
                control.FsmVariables.GetFsmFloat("AD Min X").Value = Xlevel - 12;
                control.FsmVariables.GetFsmFloat("Max X").Value = Xlevel + 15.15f;
                control.FsmVariables.GetFsmFloat("Min X").Value = Xlevel - 15.15f; 
                control.GetFirstActionOfType<SetPosition>("Balloon Pos").x.Value = Xlevel + UnityEngine.Random.Range(-15.5f, 15.5f);

                control.GetFirstActionOfType<RandomFloat>("FB Tele L").min.Value = Xlevel - 14;
                control.GetFirstActionOfType<RandomFloat>("FB Tele L").max.Value = Xlevel - 11;
                control.GetFirstActionOfType<RandomFloat>("FB Tele R").min.Value = Xlevel + 11;
                control.GetFirstActionOfType<RandomFloat>("FB Tele R").max.Value = Xlevel + 14;

                batfsm.GetFirstActionOfType<iTweenMoveTo>("Get To Middle").vectorPosition.Value = new Vector3(Xlevel, Ylevel + 1.3f, 0.005f);
                batfsm.GetAction<FloatCompare>("Fly", 3).float2.Value = Xlevel - 3;
                batfsm.GetAction<FloatCompare>("Fly", 4).float2.Value = Xlevel + 2;
                batfsm.GetAction<FloatCompare>("Fly", 5).float2.Value = Ylevel - 1.3f;
                batfsm.GetAction<FloatCompare>("Fly", 6).float2.Value = Ylevel + 1.7f;
            }
        } //Raycast the spikes from both sides, add settable spots

        static GameObject bosss = null;
        private void ModHooks_HeroUpdateHook()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                GameObject boss = GameObject.Instantiate(Nkg);
                boss.SetActive(true);
                bosss = boss;
                boss.AddComponent<NightmareKingGrimm>();
            }
/*
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                DrawLine(Yvalue + 3 + 6.7f);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                GameObject[] arr = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (GameObject obj in arr)
                {
                    if (obj.name.Contains("Fake Bat"))
                    {
                        UnityEngine.Object.Destroy(obj.GetComponent<FakeBat>());
                        obj.AddComponent<FakeBatFix>();
                        obj.AddComponent<BatLimitSetter>();
                        var fake = obj.GetComponent<FakeBatFix>();

                        FieldInfo field1 = typeof(FakeBatFix).GetField
                            (
                                "grimm",
                                BindingFlags.Instance | BindingFlags.NonPublic
                            );
                        field1.SetValue(fake, bosss.transform);
                    }
                }

                FakeBat.NotifyAllBossAwake();
                FakeBat.SendAllOut();
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                var fake = GameObject.Find("Fake Bat").GetComponent<FakeBatFix>();

                FieldInfo field1 = typeof(FakeBatFix).GetField
                    (
                        "grimm",
                        BindingFlags.Instance | BindingFlags.NonPublic
                    );
                field1.SetValue(fake, bosss.transform);


                fake.NotifyBossAwake();

                /*FieldInfo field = typeof(FakeBat).GetField
                (
                    "state",
                    BindingFlags.Instance | BindingFlags.NonPublic
                );

                //object state = field.GetValue(fake);
                //Log(state.ToString());


                fake.SendOut();
            }*/
        }

        public void DrawLine(float y)
        {
            GameObject[] arr = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach(GameObject obj in arr)
            {
                if(obj.name == "DebugLine")
                {
                    GameObject.Destroy(obj);
                }
            }

            GameObject lineObject = new GameObject("DebugLine");

            LineRenderer line = lineObject.AddComponent<LineRenderer>();

            line.material = new Material(Shader.Find("Sprites/Default"));

            line.positionCount = 2;
            line.SetPosition(0, new Vector3(-1000f, y, -1f));
            line.SetPosition(1, new Vector3(1000f, y, -1f));

            line.startWidth = 0.05f;
            line.endWidth = 0.05f;

            line.startColor = Color.red;
            line.endColor = Color.red;
        }

        public static float FindGround(float x, float y)
        {
            Vector2 Origin = new Vector2(x, y + 1);
            //Modding.Logger.Log(Origin);
            int groundLayer = LayerMask.GetMask("Terrain");
            RaycastHit2D[] ray = Physics2D.RaycastAll(Origin, Vector2.down, 500f, groundLayer);
            float output = y;

            foreach(var hit in ray)
            {
                string s = hit.collider.gameObject.name;
                if (s != "Knight" && s != "Pt Afterburn")
                {
                    output = hit.point.y;
                    break;
                }
            }

            return output;
        }

        public static bool KnightVision(Vector2 Origin, string State)
        {
            Vector2 Player = (Vector2)GameObject.Find("Knight").transform.position;
            Vector2 Direction = (Player - Origin).normalized;
            RaycastHit2D[] ray = Physics2D.RaycastAll(Origin, Direction, Vector2.Distance(Origin, Player));
            bool output = false;

            foreach (var hit in ray)
            {
                if (hit.collider.gameObject.layer == 8)
                {
                    Modding.Logger.Log("Found a wall in the way for " + State + ", not here");
                    output = true;
                    break;
                }
            }

            return output;
        }
    }
}