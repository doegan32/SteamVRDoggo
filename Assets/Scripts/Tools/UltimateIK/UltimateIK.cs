using System;
using System.Collections.Generic;
using UnityEngine;

public class UltimateIK {

    public static Model BuildModel(Transform root, params Transform[] objectives) {
        return BuildModel(null, root, objectives);
    }

    public static Model BuildModel(Model reference, Transform root, params Transform[] objectives) {
        if(reference == null) {
            Debug.Log("here 0?");
            reference = new Model();
        }
        if(reference.GetRoot() == root && reference.Objectives.Length == objectives.Length) {
            Debug.Log("here 1?");
            for(int i=0; i<objectives.Length; i++) {
                if(reference.Bones[reference.Objectives[i].Bone].Transform != objectives[i]) {
                    break;
                }
                if(i==objectives.Length-1) {
                    return reference;
                }
            }
        }
        objectives = Verify(root, objectives);
        
        for (int i=0; i < objectives.Length; i++)
        {
            Debug.Log("objectives");
            Debug.Log(objectives[i].name);
        }

        Model model = new Model();
        for(int i=0; i<objectives.Length; i++) {
            Transform[] chain = GetChain(root, objectives[i]); // list of all transforms starting with chain root and ending with chain end effector, e.g. [LeftForeArm, LeftHand, LeftHandSite]

            Debug.Log("chains");
            for (int l=0; l< chain.Length; l++)
            {
                Debug.Log(chain[l].name);
            }

            Objective objective = reference.FindObjective(objectives[i]);
            if(objective == null) {
                Debug.Log("Null in this case?");
                objective = new Objective();
            }
            for(int j=0; j<chain.Length; j++) {
                Bone bone = model.FindBone(chain[j]);
                if(bone == null) {
                    Debug.Log("AND Null in this case?");
                    bone = reference.FindBone(chain[j]);
                    if(bone != null) {
                        bone.Childs = new int[0];
                        bone.Objectives = new int[0];
                    }
                    if(bone == null) {
                        Debug.Log("AND Null in this case 2?");
                        bone = new Bone();
                        bone.Transform = chain[j];
                        bone.ZeroPosition = chain[j].localPosition;
                        Debug.Log(bone.ZeroPosition);
                        bone.ZeroRotation = chain[j].localRotation;
                        Debug.Log(bone.ZeroPosition);
                    }
                    Bone parent = model.FindBone(chain[j].parent);
                    if(parent != null) {
                        Debug.Log("We're in here now? but only twice");
                        ArrayExtensions.Append(ref parent.Childs, model.Bones.Length);
                    }
                    bone.Index = model.Bones.Length;
                    Debug.Log("Bone " + bone.Transform.name);
                    
                    ArrayExtensions.Append(ref model.Bones, bone);
                }
                ArrayExtensions.Append(ref bone.Objectives, i);
            }

            for (int k=0; k < model.Bones.Length; k++)
            {
                Debug.Log(model.Bones[k].Transform.name);
                Debug.Log(model.Bones[k].Index);
                for (int c = 0; c < model.Bones[k].Childs.Length; c++)
                {
                    Debug.Log(model.Bones[k].Childs[c]);
                }
                Debug.Log("objectives");
                for (int c = 0; c < model.Bones[k].Objectives.Length; c++)
                {
                    Debug.Log(model.Bones[k].Objectives[c]);
                }

            }

            objective.Bone = model.FindBone(chain.Last()).Index;
            objective.TargetPosition = objectives[i].transform.position;
            objective.TargetRotation = objectives[i].transform.rotation;
            objective.Index = model.Objectives.Length;
            ArrayExtensions.Append(ref model.Objectives, objective);
        }
        for (int o = 0; o < model.Objectives.Length; o++)
        {
            Debug.Log(model.Objectives[o].Index);
            Debug.Log(model.Objectives[o].Bone);
            Debug.Log(model.Objectives[o].TargetPosition);
            Debug.Log(model.Objectives[o].TargetRotation);
            Debug.Log(model.Objectives[o].SolvePosition);
            Debug.Log(model.Objectives[o].SolveRotation);
        }


        model.Iterations = reference.Iterations;
        model.Threshold = reference.Threshold;
        model.Activation = reference.Activation;
        model.AvoidBoneLimits = reference.AvoidBoneLimits;
        model.AllowRootUpdateX = reference.AllowRootUpdateX;
        model.AllowRootUpdateY = reference.AllowRootUpdateY;
        model.AllowRootUpdateZ = reference.AllowRootUpdateZ;
        model.SeedZeroPose = reference.SeedZeroPose;
        return model;
    }

    private static Transform[] Verify(Transform root, Transform[] objectives) {
        List<Transform> verified = new List<Transform>();
        if(root == null) {
            Debug.Log("Given root was null. Extracting skeleton failed.");
            return verified.ToArray();
        }
        if(objectives.Length == 0) {
            Debug.Log("No objectives given. Extracting skeleton failed.");
            return verified.ToArray();
        }
        for(int i=0; i<objectives.Length; i++) {
            if(objectives[i] == null) {
                Debug.Log("A given objective was null and will be ignored.");
            } else if(verified.Contains(objectives[i])) {
                Debug.Log("Given objective " + objectives[i].name + " is already contained and will be ignored.");
            } else if(!IsInsideHierarchy(root, objectives[i])) {
                Debug.Log("Chain for " + objectives[i].name + " is not connected to " + root.name + " and will be ignored.");
            } else {
                verified.Add(objectives[i]);
            }
        }
        return verified.ToArray();
    }

    private static Transform[] GetChain(Transform root, Transform end) {
        if(root == null || end == null) {
            return new Transform[0];
        }
        List<Transform> chain = new List<Transform>();
        Transform joint = end;
        chain.Add(joint);
        while(joint != root) {
            joint = joint.parent;
            if(joint == null) {
                return new Transform[0];
            } else {
                chain.Add(joint);
            }
        }
        chain.Reverse();
        return chain.ToArray();
    }

    public static bool IsInsideHierarchy(Transform root, Transform t) {
        if(root == null || t == null) {
            return false;
        }
        while(t != root) {
            t = t.parent;
            if(t == null) {
                return false;
            }
        }
        return true;
    }

    public enum ACTIVATION {Constant, Linear, Root, Square}
    public enum JOINT {Free, HingeX, HingeY, HingeZ, Ball}

    [System.Serializable]
    public class Model {
        public int Iterations = 25;
        public float Threshold = 0.001f;
        public ACTIVATION Activation = ACTIVATION.Linear;
        public bool SeedZeroPose = false;
        public bool AvoidBoneLimits = false;
        public bool AllowRootUpdateX = false;
        public bool AllowRootUpdateY = false;
        public bool AllowRootUpdateZ = false;

        public Bone[] Bones = new Bone[0];
        public Objective[] Objectives = new Objective[0];

        private float SolveTime = 0f;

        public bool IsSetup() {
            if(Bones.Length == 0 && Objectives.Length > 0) {
                Debug.Log("Bone count is zero but objective count is not. This should not have happened!");
                return false;
            }
            return true;
        }

        public Transform GetRoot() {
            return Bones.Length == 0 ? null : Bones.First().Transform;
        }

        public float GetSolveTime() {
            return SolveTime;
        }

        public void SetIterations(int value) {
            Iterations = Mathf.Max(value, 0);
        }

        public void SetThreshold(float value) {
            Threshold = Mathf.Max(value, 0f);
        }

        public Bone FindBone(Transform t) {
            return Array.Find(Bones, x => x.Transform == t);
        }

        public Objective FindObjective(Transform t) {
            return Array.Find(Objectives, x => x.Bone < Bones.Length && Bones[x.Bone].Transform == t);
        }

        public void SaveAsZeroPose() {
            foreach(Bone bone in Bones) {
                bone.ZeroPosition = bone.Transform.localPosition;
                bone.ZeroRotation = bone.Transform.localRotation;
            }
        }

        public void SetWeights(params float[] weights) {
            if(Objectives.Length != weights.Length) {
                Debug.Log("Number of given weights <" + weights.Length + "> does not match number of objectives <" + Objectives.Length + ">.");
                return;
            }
            for(int i=0; i<Objectives.Length; i++) {
                Objectives[i].Weight = weights[i];
            }
        }

        public void SetTargets(params Matrix4x4[] targets) {
            if(Objectives.Length != targets.Length) {
                Debug.Log("Number of given targets <" + targets.Length + "> does not match number of objectives <" + Objectives.Length + ">.");
                return;
            }
            for(int i=0; i<Objectives.Length; i++) {
                Objectives[i].TargetPosition = targets[i].GetPosition();
                Objectives[i].TargetRotation = targets[i].GetRotation();
            }
        }

        public void Solve() {
            DateTime timestamp = Utility.GetTimestamp();
            if(IsSetup()) {
                //Compute Levels
                Action<Bone, Bone> recursion = null;
                recursion = new Action<Bone, Bone>((parent, bone) => {
                    bone.Level = parent == null ? 1 : bone.Active && parent.Active ? parent.Level + 1 : parent.Level;
                    foreach(int index in bone.Childs) {
                        recursion(bone, Bones[index]);
                    }
                });
                recursion(null, Bones.First());

                //Debug.Log("Levels");
                //for (int b = 0; b < Bones.Length; b++)
                //{
                //    Debug.Log(Bones[b].Level);
                //}
                // first bone in chain is level 1 and so on down 

                //Apply Zero Pose
                if(SeedZeroPose) {
                    Debug.Log("we don't SeedZeroPose");
                    foreach(Bone bone in Bones) {
                        if(bone.Active) {
                            bone.Transform.localPosition = bone.ZeroPosition;
                            bone.Transform.localRotation = bone.ZeroRotation;
                        }
                    }
                }

                //Solve IK
                for(int i=0; i<Iterations; i++) {
                    if(!IsConverged()) {
                        if(AllowRootUpdateX || AllowRootUpdateY || AllowRootUpdateZ) {
                            Debug.Log("we don't allowrootupdate");
                            Vector3 delta = Vector3.zero;
                            int count = 0;
                            foreach(Objective o in Objectives) {
                                if(o.Active) {
                                    delta += GetWeight(Bones[o.Bone], o) * (o.TargetPosition - Bones[o.Bone].Transform.position);
                                    count += 1;
                                }
                            }
                            delta.x *= AllowRootUpdateX ? 1f : 0f;
                            delta.y *= AllowRootUpdateY ? 1f : 0f;
                            delta.z *= AllowRootUpdateZ ? 1f : 0f;
                            if(count > 0) {
                                GetRoot().position += delta / count;
                            }
                        }
                        //Debug.Log("Optimizw");
                        Optimise(Bones.First());
                    }
                }
            }
            SolveTime = (float)Utility.GetElapsedTime(timestamp);

            bool IsConverged() {
                foreach(Objective o in Objectives) {
                    if(o.GetError(this) > Threshold) {
                        return false;
                    }
                }
                return true;
            }

            float GetWeight(Bone bone, Objective objective) {
                float weightSum = 0f;
                for(int i=0; i<Objectives.Length; i++) {
                    weightSum += Objectives[i].Weight;
                }
                float weight = (weightSum > 0f && weightSum < Objectives.Length) ? (objective.Weight * Objectives.Length / weightSum) : 1f;
                switch(Activation) {
                    case ACTIVATION.Constant:
                    return weight;
                    case ACTIVATION.Linear:
                    return weight * (float)bone.Level/(float)Bones[objective.Bone].Level;
                    case ACTIVATION.Root:
                    return Mathf.Sqrt(weight * (float)bone.Level/(float)Bones[objective.Bone].Level);
                    case ACTIVATION.Square:
                    return Mathf.Pow(weight * (float)bone.Level/(float)Bones[objective.Bone].Level, 2f);
                    default:
                    return 1f;
                }
            }

            void Optimise(Bone bone) {
                
               // Debug.Log(bone.Transform.name);
                if(bone.Active) {
                    Vector3 pos = bone.Transform.position;
                    Quaternion rot = bone.Transform.rotation;
                    Vector3 forward = Vector3.zero;
                    Vector3 up = Vector3.zero;
                    int count = 0;

                

                    //Solve Objective Rotations
                    foreach(int index in bone.Objectives) {
                        Objective o = Objectives[index];
                        if(o.Active && o.SolveRotation) {
                            Quaternion q = Quaternion.Slerp(
                                rot,
                                o.TargetRotation * Quaternion.Inverse(Bones[o.Bone].Transform.rotation) * rot,
                                GetWeight(bone, o)
                            );
                            forward += q*Vector3.forward;
                            up += q*Vector3.up;
                            count += 1;
                        }
                    }

                    //Solve Objective Positions
                    foreach(int index in bone.Objectives) {
                        Objective o = Objectives[index];
                        //Debug.Log("O bone");
                        //Debug.Log(Bones[o.Bone].Transform.name);
                        //Debug.Log("O weight");
                        //Debug.Log(GetWeight(bone, o));
                        if (o.Active && o.SolvePosition) {
                            Quaternion q = Quaternion.Slerp(
                                rot,
                                Quaternion.FromToRotation(Bones[o.Bone].Transform.position - pos, o.TargetPosition - pos) * rot,
                                GetWeight(bone, o)
                            );
                            forward += q*Vector3.forward;
                            up += q*Vector3.up;
                            count += 1;
                        }
                    }

                    if(count > 0) {
                        bone.Transform.rotation = Quaternion.LookRotation((forward/count).normalized, (up/count).normalized);
                        bone.ResolveLimits(AvoidBoneLimits);
                    }

                    Debug.Log("Count " + count.ToString());
                }

                

                foreach(int index in bone.Childs) {
                    Optimise(Bones[index]);
                }
            }
        }
    }

    [System.Serializable]
    public class Bone {
        public int Index = 0;
        public bool Active = true;

        public Transform Transform = null;
        
        [SerializeField] private JOINT Joint = JOINT.Free;
        [SerializeField] private float LowerLimit = 0f;
        [SerializeField] private float UpperLimit = 0f;

        public int Level = 0;
        public int[] Childs = new int[0];
        public int[] Objectives = new int[0];

        public Vector3 ZeroPosition;
        public Quaternion ZeroRotation;

        public void SetJoint(JOINT joint) {
            if(Joint != joint) {
                Joint = joint;
                LowerLimit = 0f;
                UpperLimit = 0f;
            }
        }

        public JOINT GetJoint() {
            return Joint;
        }

        public void SetLowerLimit(float value) {
            LowerLimit = Mathf.Clamp(value, -180f, 0f);
        }

        public float GetLowerLimit() {
            return LowerLimit;
        }

        public void SetUpperLimit(float value) {
            UpperLimit = Mathf.Clamp(value, 0f, 180f);
        }

        public float GetUpperLimit() {
            return UpperLimit;
        }

        public void ResolveLimits(bool avoidBoneLimits) {
            switch(Joint) {
                case JOINT.Free:
                    Debug.Log("All our joints are free???");
                break;

                case JOINT.HingeX:
                {
                    float angle = Vector3.SignedAngle(ZeroRotation.GetForward(), Vector3.ProjectOnPlane(Transform.localRotation.GetForward(), ZeroRotation.GetRight()), ZeroRotation.GetRight());
                    Transform.localRotation = ZeroRotation * (avoidBoneLimits ?
                    Quaternion.AngleAxis(Mathf.Repeat(angle-LowerLimit, UpperLimit-LowerLimit) + LowerLimit, Vector3.right):
                    Quaternion.AngleAxis(Mathf.Clamp(angle, LowerLimit, UpperLimit), Vector3.right));
                }
                break;

                case JOINT.HingeY:
                {
                    float angle = Vector3.SignedAngle(ZeroRotation.GetRight(), Vector3.ProjectOnPlane(Transform.localRotation.GetRight(), ZeroRotation.GetUp()), ZeroRotation.GetUp());
                    Transform.localRotation = ZeroRotation * (avoidBoneLimits ?
                    Quaternion.AngleAxis(Mathf.Repeat(angle-LowerLimit, UpperLimit-LowerLimit) + LowerLimit, Vector3.up):
                    Quaternion.AngleAxis(Mathf.Clamp(angle, LowerLimit, UpperLimit), Vector3.up));
                }
                break;

                case JOINT.HingeZ:
                {
                    float angle = Vector3.SignedAngle(ZeroRotation.GetUp(), Vector3.ProjectOnPlane(Transform.localRotation.GetUp(), ZeroRotation.GetForward()), ZeroRotation.GetForward());
                    Transform.localRotation = ZeroRotation * (avoidBoneLimits ?
                    Quaternion.AngleAxis(Mathf.Repeat(angle-LowerLimit, UpperLimit-LowerLimit) + LowerLimit, Vector3.forward):
                    Quaternion.AngleAxis(Mathf.Clamp(angle, LowerLimit, UpperLimit), Vector3.forward));
                }
                break;

                case JOINT.Ball:
                {
                    if(UpperLimit == 0f) {
                        Transform.localRotation = ZeroRotation;
                    } else {
                        Quaternion current = Transform.localRotation;
                        float angle = Quaternion.Angle(ZeroRotation, current);
                        if(angle > UpperLimit) {
                            Transform.localRotation = Quaternion.Slerp(ZeroRotation, current, UpperLimit / angle);
                        }
                    }
                }
                break;
            }
        }

    }

    [System.Serializable]
    public class Objective {
        public int Index = 0;
        public bool Active = true;
        public int Bone = 0;
        public Vector3 TargetPosition = Vector3.zero;
        public Quaternion TargetRotation = Quaternion.identity;
        public float Weight = 1f;
        public bool SolvePosition = true;
        public bool SolveRotation = true;

        public void SetTarget(Matrix4x4 matrix) {
            SetTarget(matrix.GetPosition());
            SetTarget(matrix.GetRotation());
        }

        public void SetTarget(Transform transform) {
            TargetPosition = transform.position;
            TargetRotation = transform.rotation;
        }

        public void SetTarget(Vector3 position) {
            TargetPosition = position;
        }

        public void SetTarget(Quaternion rotation) {
            TargetRotation = rotation;
        }

        public float GetError(Model model) {
            if(!Active) {
                return 0f;
            }
            float error = 0f;
            if(SolvePosition) {
                error += Vector3.Distance(model.Bones[Bone].Transform.position, TargetPosition);
            }
            if(SolveRotation) {
                error += Mathf.Deg2Rad * Quaternion.Angle(model.Bones[Bone].Transform.rotation, TargetRotation);
            }
            return error;
        }
    }
    
}