using System;
using System.Collections.Generic;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Graphics;
using FlatRedBall.Instructions.Pause;
using FlatRedBall.Instructions.Interpolation;
using System.Reflection;

using FlatRedBall.Math;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Threading.Tasks;
using System.Threading;

namespace FlatRedBall.Instructions
{
    #region InterpolationType Enum
    public enum InterpolatorType
    {
        Linear,
        ClosestRotation
    }
    #endregion

    public static class InstructionManager
    {


        private static Lazy<InstructionManagerData> Data = new Lazy<InstructionManagerData>(() => new InstructionManagerData());

        private class InstructionManagerData
        {
            #region SingleTon

            public InstructionManagerData()
            {
                CreateInterpolators();

                CreateValueRelationships();

                CreateRotationMembers();
            }

            #region Private Methods

            private void CreateInterpolators()
            {
                _mInterpolators.Add(typeof(float), new FloatInterpolator());
                _mInterpolators.Add(typeof(double), new DoubleInterpolator());
                _mInterpolators.Add(typeof(long), new LongInterpolator());
                _mInterpolators.Add(typeof(int), new IntInterpolator());

                _mRotationInterpolators.Add(typeof(float), new FloatAngleInterpolator());
            }

            private void CreateRotationMembers()
            {
                _mRotationMembers.Add("RotationX");
                _mRotationMembers.Add("RotationY");
                _mRotationMembers.Add("RotationZ");

                _mRotationMembers.Add("RelativeRotationX");
                _mRotationMembers.Add("RelativeRotationY");
                _mRotationMembers.Add("RelativeRotationZ");

            }

            private void CreateValueRelationships()
            {
                #region Create the Value Relationships

                _mVelocityValueRelationships.Add(new VelocityValueRelationship("X", "XVelocity", "XAcceleration"));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("Y", "YVelocity", "YAcceleration"));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("Z", "ZVelocity", "ZAcceleration"));

                _mVelocityValueRelationships.Add(new VelocityValueRelationship("RelativeX", "RelativeXVelocity",
                    "RelativeXAcceleration"));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("RelativeY", "RelativeYVelocity",
                    "RelativeYAcceleration"));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("RelativeZ", "RelativeZVelocity",
                    "RelativeZAcceleration"));

                _mVelocityValueRelationships.Add(new VelocityValueRelationship("ScaleX", "ScaleXVelocity", null));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("ScaleY", "ScaleYVelocity", null));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("ScaleZ", "ScaleZVelocity", null));

                _mVelocityValueRelationships.Add(new VelocityValueRelationship("Radius", "RadiusVelocity", null));

                _mVelocityValueRelationships.Add(new VelocityValueRelationship("Alpha", "AlphaRate", null));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("Red", "RedRate", null));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("Green", "GreenRate", null));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("Blue", "BlueRate", null));

                _mVelocityValueRelationships.Add(new VelocityValueRelationship("RotationX", "RotationXVelocity", null));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("RotationY", "RotationYVelocity", null));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("RotationZ", "RotationZVelocity", null));

                _mVelocityValueRelationships.Add(new VelocityValueRelationship("RelativeRotationX",
                    "RelativeRotationXVelocity", null));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("RelativeRotationY",
                    "RelativeRotationYVelocity", null));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("RelativeRotationZ",
                    "RelativeRotationZVelocity", null));


                _mVelocityValueRelationships.Add(new VelocityValueRelationship("LeftDestination",
                    "LeftDestinationVelocity", null));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("RightDestination",
                    "RightDestinationVelocity", null));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("TopDestination",
                    "TopDestinationVelocity", null));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("BottomDestination",
                    "BottomDestinationVelocity", null));

                _mVelocityValueRelationships.Add(new VelocityValueRelationship("Scale", "ScaleVelocity", null));
                _mVelocityValueRelationships.Add(new VelocityValueRelationship("Spacing", "SpacingVelocity", null));

                #endregion

                #region Create the Animation Relationships

                _mAnimationValueRelationships.Add(new AnimationValueRelationship("CurrentFrameIndex", "AnimationSpeed",
                    "CurrentChain", "Count"));

                #endregion

                #region Create the Absolute/Relative Relationships

                _mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("X", "RelativeX"));
                _mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("Y", "RelativeY"));
                _mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("Z", "RelativeZ"));

                _mAbsoluteRelativeValueRelationships.Add(
                    new AbsoluteRelativeValueRelationship("XVelocity", "RelativeXVelocity"));
                _mAbsoluteRelativeValueRelationships.Add(
                    new AbsoluteRelativeValueRelationship("YVelocity", "RelativeYVelocity"));
                _mAbsoluteRelativeValueRelationships.Add(
                    new AbsoluteRelativeValueRelationship("ZVelocity", "RelativeZVelocity"));

                _mAbsoluteRelativeValueRelationships.Add(
                    new AbsoluteRelativeValueRelationship("RotationX", "RelativeRotationX"));
                _mAbsoluteRelativeValueRelationships.Add(
                    new AbsoluteRelativeValueRelationship("RotationY", "RelativeRotationY"));
                _mAbsoluteRelativeValueRelationships.Add(
                    new AbsoluteRelativeValueRelationship("RotationZ", "RelativeRotationZ"));

                _mAbsoluteRelativeValueRelationships.Add(
                    new AbsoluteRelativeValueRelationship("RotationXVelocity", "RelativeRotationXVelocity"));
                _mAbsoluteRelativeValueRelationships.Add(
                    new AbsoluteRelativeValueRelationship("RotationYVelocity", "RelativeRotationYVelocity"));
                _mAbsoluteRelativeValueRelationships.Add(
                    new AbsoluteRelativeValueRelationship("RotationZVelocity", "RelativeRotationZVelocity"));

                _mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("Top", "RelativeTop"));
                _mAbsoluteRelativeValueRelationships.Add(
                    new AbsoluteRelativeValueRelationship("Bottom", "RelativeBottom"));
                _mAbsoluteRelativeValueRelationships.Add(new AbsoluteRelativeValueRelationship("Left", "RelativeLeft"));
                _mAbsoluteRelativeValueRelationships.Add(
                    new AbsoluteRelativeValueRelationship("Right", "RelativeRight"));

                #endregion

#if !SILVERLIGHT
                VelocityValueRelationships =
                    new ReadOnlyCollection<VelocityValueRelationship>(_mVelocityValueRelationships);
                AbsoluteRelativeValueRelationships =
                    new ReadOnlyCollection<AbsoluteRelativeValueRelationship>(_mAbsoluteRelativeValueRelationships);
#endif
            }

            #endregion

            #endregion

            #region Fields

            private readonly object _syncLock = new object();

            private readonly Queue<Instruction> _instructionQueue = new Queue<Instruction>();

            private bool _mIsExecutingInstructions = true;

            private readonly InstructionList _mInstructions = new InstructionList();

            private readonly InstructionList _mUnpauseInstructions = new InstructionList();

            private readonly Dictionary<Type, IInterpolator> _mInterpolators = new Dictionary<Type, IInterpolator>();

            private readonly Dictionary<Type, IInterpolator> _mRotationInterpolators = new Dictionary<Type, IInterpolator>();

            private readonly List<VelocityValueRelationship> _mVelocityValueRelationships = new List<VelocityValueRelationship>();

            private readonly List<AnimationValueRelationship> _mAnimationValueRelationships = new List<AnimationValueRelationship>();

            private readonly List<AbsoluteRelativeValueRelationship> _mAbsoluteRelativeValueRelationships = new List<AbsoluteRelativeValueRelationship>();

            private readonly List<string> _mRotationMembers = new List<string>();

            #endregion

            #region Properties
   
            public InstructionList Instructions
            {
                get { return _mInstructions; }
            }

            public bool IsEnginePaused => _mUnpauseInstructions.Count != 0;

            public bool IsExecutingInstructions
            {
                get { return _mIsExecutingInstructions; }
                set { _mIsExecutingInstructions = value; }
            }

            public int UnpauseInstructionCount => _mUnpauseInstructions.Count;

            public ReadOnlyCollection<VelocityValueRelationship> VelocityValueRelationships { get; private set; }
            public ReadOnlyCollection<AbsoluteRelativeValueRelationship> AbsoluteRelativeValueRelationships { get; private set; }

            #endregion

            #region Methods

            #region Public Methods

            public void AddSafe(Instruction instruction)
            {
                lock (_syncLock)
                {
                    _instructionQueue.Enqueue(instruction);
                }
            }

            public void AddSafe(Action action)
            {
                lock (_syncLock)
                {
                    _instructionQueue.Enqueue(new DelegateInstruction(action));
                }
            }

            public async Task DoOnMainThreadAsync(Action action)
            {
                var semaphor = new SemaphoreSlim(1);
                semaphor.Wait();

                AddSafe(() =>
                {
                    action();
                    semaphor.Release();
                });

                await semaphor.WaitAsync();

            }

            public void Add(Instruction instruction)
            {
                _mInstructions.Add(instruction);
            }
            public void ExecuteInstructionsOnConsideringTime(IInstructable instructable)
            {
                ExecuteInstructionsOnConsideringTime(instructable, TimeManager.CurrentTime);
            }

            public void ExecuteInstructionsOnConsideringTime(IInstructable instructable, double currentTime)
            {
                var instructions = instructable.Instructions;
#if DEBUG
                if (instructions == null)
                {
                    throw new InvalidOperationException(
                        $"The instructable of type {instructable.GetType()} has null instructions. These should be instantiated before attempting to execute instructions");
                }
#endif

                while (instructions.Count > 0 && instructions[0].TimeToExecute <= currentTime)
                {
                    var instruction = instructions[0];
                    instruction.Execute();

                    // The instruction may have cleared the InstructionList, so we need to test if it did.
                    if (instructions.Count < 1)
                        continue;

                    if (instruction.CycleTime == 0)
                        instructions.Remove(instruction);
                    else
                    {
                        instruction.TimeToExecute += instruction.CycleTime;
                        instructions.InsertionSortAscendingTimeToExecute();
                    }
                }
            }

            public Type GetTypeForMember(Type type, string member)
            {
                PropertyInfo propertyInfo = type.GetProperty(member);

                if (propertyInfo != null)
                {
                    return propertyInfo.PropertyType;
                }

                FieldInfo fieldInfo = type.GetField(member);

                if (fieldInfo != null)
                {
                    return fieldInfo.FieldType;
                }

                return null;

            }

            public AnimationValueRelationship GetAnimationValueRelationship(string frameMemberName)
            {
                for (int i = 0; i < _mAnimationValueRelationships.Count; i++)
                {
                    if (_mAnimationValueRelationships[i].Frame == frameMemberName)
                    {
                        return _mAnimationValueRelationships[i];
                    }
                }
                return null;
            }

            public string GetStateForVelocity(string velocity)
            {
                for (int i = 0; i < _mVelocityValueRelationships.Count; i++)
                {
                    if (_mVelocityValueRelationships[i].Velocity == velocity)
                        return _mVelocityValueRelationships[i].State;
                }

                return null;
            }

            public string GetRelativeForAbsolute(string absolute)
            {
                return (from t
                    in _mAbsoluteRelativeValueRelationships
                        where String.Equals(t.AbsoluteValue, absolute, StringComparison.OrdinalIgnoreCase)
                        select t.RelativeValue).FirstOrDefault();
            }

            public string GetVelocityForState(string state)
            {
                return (from t
                    in _mVelocityValueRelationships
                        where String.Equals(t.State, state, StringComparison.OrdinalIgnoreCase)
                        select t.Velocity).FirstOrDefault();
            }

            public bool IsObjectReferencedByInstructions(object objectToReference)
            {
                return _mInstructions.Any(t => Equals(t.Target, objectToReference));
            }

            public bool IsRotationMember(string rotationMember)
            {
                return _mRotationMembers.Contains(rotationMember);
            }

            #region Move 

            public void MoveThrough<T>(PositionedObject positionedObject, IList<T> list, float velocity) where T : IPositionable
            {
                var time = TimeManager.CurrentTime;

                var lastX = positionedObject.X;
                var lastY = positionedObject.Y;
                var lastZ = positionedObject.Z;

                var newVelocity = new Vector3();

                foreach (var positionable in list)
                {
                    var distanceX = positionable.X - lastX;
                    var distanceY = positionable.Y - lastY;
                    var distanceZ = positionable.Z - lastZ;

                    double totalDistance = (float)System.Math.Sqrt(distanceX * distanceX + distanceY * distanceY + distanceZ * distanceZ);

                    newVelocity.X = distanceX;
                    newVelocity.Y = distanceY;
                    newVelocity.Z = distanceZ;

                    newVelocity.Normalize();
                    newVelocity *= velocity;

                    positionedObject.Instructions.Add(new Instruction<PositionedObject, Vector3>(positionedObject, "Velocity", newVelocity, time));

                    lastX = positionable.X;
                    lastY = positionable.Y;
                    lastZ = positionable.Z;

                    time += totalDistance / velocity;

                }

                positionedObject.Instructions.Add(new Instruction<PositionedObject, Vector3>(positionedObject, "Velocity", new Vector3(), time));
            }


            public void MoveToAccurate(FlatRedBall.PositionedObject positionedObject, float x, float y, float z, double secondsToTake)
            {
                if (secondsToTake != 0.0f)
                {
                    positionedObject.XVelocity = (x - positionedObject.X) / (float)secondsToTake;
                    positionedObject.YVelocity = (y - positionedObject.Y) / (float)secondsToTake;
                    positionedObject.ZVelocity = (z - positionedObject.Z) / (float)secondsToTake;

                    double timeToExecute = TimeManager.CurrentTime + secondsToTake;

                    positionedObject.Instructions.Add(new Instruction<PositionedObject, float>(positionedObject, "XVelocity", 0, timeToExecute));
                    positionedObject.Instructions.Add(new Instruction<PositionedObject, float>(positionedObject, "YVelocity", 0, timeToExecute));
                    positionedObject.Instructions.Add(new Instruction<PositionedObject, float>(positionedObject, "ZVelocity", 0, timeToExecute));

                    positionedObject.Instructions.Add(new Instruction<PositionedObject, float>(positionedObject, "X", x, timeToExecute));
                    positionedObject.Instructions.Add(new Instruction<PositionedObject, float>(positionedObject, "Y", y, timeToExecute));
                    positionedObject.Instructions.Add(new Instruction<PositionedObject, float>(positionedObject, "Z", z, timeToExecute));
                }
                else
                {
                    positionedObject.X = x;
                    positionedObject.Y = y;
                    positionedObject.Z = z;
                }
            }

            public void MoveTo(PositionedObject positionedObject, float x, float y, float z, double secondsToTake)
            {
                if (secondsToTake != 0.0f)
                {
                    positionedObject.XVelocity = (x - positionedObject.X) / (float)secondsToTake;
                    positionedObject.YVelocity = (y - positionedObject.Y) / (float)secondsToTake;
                    positionedObject.ZVelocity = (z - positionedObject.Z) / (float)secondsToTake;

                    var timeToExecute = TimeManager.CurrentTime + secondsToTake;

                    positionedObject.Instructions.Add(new Instruction<PositionedObject, float>(positionedObject, "XVelocity", 0, timeToExecute));
                    positionedObject.Instructions.Add(new Instruction<PositionedObject, float>(positionedObject, "YVelocity", 0, timeToExecute));
                    positionedObject.Instructions.Add(new Instruction<PositionedObject, float>(positionedObject, "ZVelocity", 0, timeToExecute));
                }
                else
                {
                    positionedObject.X = x;
                    positionedObject.Y = y;
                    positionedObject.Z = z;
                }
            }

            #endregion

            #region Pause Methods

            internal PositionedObjectList<FlatRedBall.PositionedObject> PositionedObjectsIgnoringPausing =
                new PositionedObjectList<FlatRedBall.PositionedObject>();

            public List<object> ObjectsIgnoringPausing { get; private set; } = new List<object>();

            public void IgnorePausingFor(FlatRedBall.PositionedObject positionedObject)
            {
                // This function needs to tolerate
                // the same object being added multiple
                // times.  The reason is that an Entity in
                // Glue may set one of its objects to be ignored
                // in pausing, but then the object itself may also
                // be set to be ignored.
                if (!PositionedObjectsIgnoringPausing.Contains(positionedObject))
                {
                    PositionedObjectsIgnoringPausing.Add(positionedObject);
                }
            }

            public void IgnorePausingFor<T>(IList<T> list) where T : FlatRedBall.PositionedObject
            {
                for (int i = 0; i < list.Count; i++)
                {
                    IgnorePausingFor(list[i]);
                }
            }


            public void IgnorePausingFor(Scene scene)
            {

                IgnorePausingFor(scene.SpriteFrames);
                IgnorePausingFor(scene.Sprites);
                IgnorePausingFor(scene.Texts);
            }

            public void IgnorePausingFor(ShapeCollection shapeCollection)
            {
                IgnorePausingFor(shapeCollection.AxisAlignedCubes);
                IgnorePausingFor(shapeCollection.AxisAlignedRectangles);
                IgnorePausingFor(shapeCollection.Capsule2Ds);
                IgnorePausingFor(shapeCollection.Circles);
                IgnorePausingFor(shapeCollection.Lines);
                IgnorePausingFor(shapeCollection.Polygons);
                IgnorePausingFor(shapeCollection.Spheres);
            }
            
            void IgnorePausingFor<T>(PositionedObjectList<T> list) where T : FlatRedBall.PositionedObject
            {
                int count = list.Count;
                for (int i = 0; i < count; i++)
                {
                    IgnorePausingFor(list[i]);
                }
            }

            public void PauseEngine(bool storeUnpauseInstructions)
            {
                if (_mUnpauseInstructions.Count != 0)
                {
                    throw new System.InvalidOperationException(
                        "Can't execute pause since there are already instructions in the Unpause InstructionList." +
                        " Are you causing PauseEngine twice in a row?" +
                        " Each PauseEngine method must be followed by an UnpauseEngine before PauseEngine can be called again.");

                }
                // When the engine pauses, each manager stops 
                // all activity and fills the unpauseInstructions
                // with instructions that are executed to restart activity.

                // Turn off sorting so we don't sort over and over and over...
                // Looks like we never need to turn this back on.  
                _mUnpauseInstructions.SortOnAdd = false;

                SpriteManager.Pause(_mUnpauseInstructions);

                ShapeManager.Pause(_mUnpauseInstructions);

                TextManager.Pause(_mUnpauseInstructions);

                InstructionListUnpause unpauseInstruction = new InstructionListUnpause(_mInstructions);
                _mInstructions.Clear();

                // the minority of instructions should be targeting objects that can't be paused, so loop through
                // and pull them back out
                for (int i = unpauseInstruction.TemporaryInstructions.Count - 1; i > -1; i--)
                {
                    var instruction = unpauseInstruction.TemporaryInstructions[i];

                    if (ObjectsIgnoringPausing.Contains(instruction.Target))
                    {
                        unpauseInstruction.TemporaryInstructions.RemoveAt(i);
                        // add it back!
                        _mInstructions.Add(instruction);
                    }
                }


                _mUnpauseInstructions.Add(unpauseInstruction);

                if (!storeUnpauseInstructions)
                {
                    _mUnpauseInstructions.Clear();
                }

                // ... now do one sort at the end to make sure all is sorted properly
                // Actually, no, don't sort, it's not necessary!  We're going to execute
                // everything anyway.
                //mUnpauseInstructions.Sort((a, b) => a.TimeToExecute.CompareTo(b));
            }

            public void FindAndExecuteUnpauseInstruction(object target)
            {
                for (int iCurInstruction = _mUnpauseInstructions.Count - 1; iCurInstruction > -1; iCurInstruction--)
                {
                    if (_mUnpauseInstructions[iCurInstruction].Target == target)
                    {
                        _mUnpauseInstructions[iCurInstruction].Execute();
                        _mUnpauseInstructions.RemoveAt(iCurInstruction);
                        break;
                    }
                }
            }

            public void UnpauseEngine()
            {
                foreach (Instruction instruction in _mUnpauseInstructions)
                {
                    instruction.Execute();
                }

                _mUnpauseInstructions.Clear();

            }

            #endregion

            public void Remove(Instruction instruction)
            {
                _mInstructions.Remove(instruction);
            }

            public void UncastedSetMember(object objectToSetOn, string memberName, object valueToSet)
            {
                if (objectToSetOn == null)
                {
                    throw new ArgumentNullException($"Argument {objectToSetOn} cannot be null");
                }

                var typeOfObject = objectToSetOn.GetType();
                var properties = typeOfObject.GetProperties();

                foreach (var propertyInfo in properties)
                {
                    if (propertyInfo.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase))
                    {
                        propertyInfo.SetValue(objectToSetOn, valueToSet, null);
                        return; // end here since it's been set - there's no reason to keep going on to fields
                    }
                }

                var fields = typeOfObject.GetFields();

                foreach (var field in fields)
                {
                    if (field.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase))
                    {
                        field.SetValue(objectToSetOn, valueToSet);
                    }
                }

            }

            #endregion

            internal IInterpolator GetInterpolator(Type type)
            {
                return GetInterpolator(type, InterpolatorType.Linear);
            }

            internal IInterpolator GetInterpolator(Type type, string memberName)
            {
                InterpolatorType interpolatorType = InterpolatorType.Linear;

                if (_mRotationMembers.Contains(memberName))
                    interpolatorType = InterpolatorType.ClosestRotation;

                return GetInterpolator(type, interpolatorType);
            }

            internal IInterpolator GetInterpolator(Type type, InterpolatorType interpolatorType)
            {
                Dictionary<Type, IInterpolator> interpolatorDictionary;

                switch (interpolatorType)
                {
                    case InterpolatorType.Linear:
                        interpolatorDictionary = _mInterpolators;
                        break;
                    case InterpolatorType.ClosestRotation:
                        interpolatorDictionary = _mRotationInterpolators;
                        break;
                    default: throw new NotImplementedException();
                }

                if (interpolatorDictionary.TryGetValue(type, out var interpolator))
                {
                    return interpolator;
                }

                throw new InvalidOperationException("There is no interpolator registered for type " + type.ToString());
            }

            internal bool HasInterpolatorForType(Type type)
            {
                return _mInterpolators.ContainsKey(type);
            }

            internal void Update()
            {
                //Flush();

                var currentTime = TimeManager.CurrentTime;

                Instruction instruction;

                lock (_syncLock)
                {
                    while (_instructionQueue.Count > 0)
                    {
                        Add(_instructionQueue.Dequeue());
                    }
                }

                while (_mIsExecutingInstructions &&
                    _mInstructions.Count > 0 &&
                    _mInstructions[0].TimeToExecute <= currentTime)
                {
                    instruction = _mInstructions[0];

                    // Nov 2, 2019
                    // An instruction 
                    // may pause the game, 
                    // which would then take 
                    // this instruction and put
                    // it on the to-execute list 
                    // of instructions. But since 
                    // it's already executing we don't
                    // want that to happen, so going to 
                    // remove it before executing. This is
                    // different from how the engine used to
                    // work prior to this date, so hopefully this
                    // doesn't introduce any weird behaviors

                    if (instruction.CycleTime == 0)
                        _mInstructions.Remove(instruction);


                    instruction.Execute();

                    // The instruction may have cleared the InstructionList, so we need to test if it did.
                    if (_mInstructions.Count < 1)
                        continue;

                    if (instruction.CycleTime != 0)
                    {
                        instruction.TimeToExecute += instruction.CycleTime;
                        _mInstructions.InsertionSortAscendingTimeToExecute();
                    }
                }

                // The ScreenManager doesn't have any engine-initiated
                // activity, it's all initiated by custom code.  However,
                // instructions are supposed to execute before any custom code
                // runs.  Therefore, we're going to have these be handled here:
                if (Screens.ScreenManager.CurrentScreen != null)
                {
                    ExecuteInstructionsOnConsideringTime(Screens.ScreenManager.CurrentScreen);
                }

            }
            #endregion
        }

        #region Properties

        /// <summary>
        /// Holds instructions which will be executed by the InstructionManager
        /// in its Update method (called automatically by FlatRedBallServices).
        /// This list is sorted by time.
        /// </summary>
        /// <remarks>
        /// Instructions for managed PositionedObjects like Sprites and Text objects
        /// should be added to the object's internal InstructionList.  This prevents instructions
        /// from referencing removed objects and helps with debugging.  This list should only be used
        /// on un-managed objects or for instructions which do not associate with a particular object.
        /// </remarks>        
        public static InstructionList Instructions => Data.Value.Instructions;

        public static bool IsEnginePaused => Data.Value.IsEnginePaused;

        /// <summary>
        /// Whether the (automatically called) Update method executes instructions.  Default true.
        /// </summary>
        public static bool IsExecutingInstructions
        {
            get => Data.Value.IsExecutingInstructions;
            set => Data.Value.IsExecutingInstructions = value;
        }

        public static int UnpauseInstructionCount => Data.Value.UnpauseInstructionCount;

        public static ReadOnlyCollection<VelocityValueRelationship> VelocityValueRelationships => Data.Value.VelocityValueRelationships;
        public static ReadOnlyCollection<AbsoluteRelativeValueRelationship> AbsoluteRelativeValueRelationships => Data.Value.AbsoluteRelativeValueRelationships;

        #endregion

        #region Methods

        #region Public Methods


        /// <summary>
        /// Adds the instruction to be executed
        /// on the next frame on the primary thread.
        /// </summary>
        /// <param name="instruction">The instruction to execute.</param>
        public static void AddSafe(Instruction instruction)
        {
            Data.Value.AddSafe(instruction);
        }

        /// <summary>
        /// Creates a new DelegateInstruction using the argument Action, and adds it to be executed
        /// on the next frame on the primary thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public static void AddSafe(Action action)
        {
            Data.Value.AddSafe(action);
        }

        /// <summary>
        /// Performs the argument action on the main thread and awaits its execution. 
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <returns>A task which can be awaited.</returns>
        public static async Task DoOnMainThreadAsync(Action action)
        {
            Data.Value.DoOnMainThreadAsync(action);
        }

        /// <summary>
        /// Adds the argument instruction to the InstructionManager, to be executed when its time is reached.
        /// </summary>
        /// <param name="instruction">The instruction to remove</param>
        public static void Add(Instruction instruction)
        {
            Data.Value.Add(instruction);
        }

        /// <summary>
        /// Attempts to execute instructions held by the argument instructable according to the TimeManager.CurrentTime.
        /// Executed instructions will either be removed or cycled if the CycleTime is greater than 0.
        /// </summary>
        /// <param name="instructable">The instructable to execute instructions on.</param>
        public static void ExecuteInstructionsOnConsideringTime(IInstructable instructable)
        {
            Data.Value.ExecuteInstructionsOnConsideringTime(instructable, TimeManager.CurrentTime);
        }


        /// <summary>
        /// Attempts to execute instructions held by the argument instruct
        /// able according to the currentTime value.
        /// Executed instructions will either be removed or cycled if the CycleTime is greater than 0.
        /// </summary>
        /// <param name="instructable">The instructable to execute instructions on.</param>
        /// <param name="currentTime">The time to compare to instructions in the instructable instance.</param>
        public static void ExecuteInstructionsOnConsideringTime(IInstructable instructable, double currentTime)
        {
            Data.Value.ExecuteInstructionsOnConsideringTime(instructable, currentTime);
        }

        public static Type GetTypeForMember(Type type, string member)
        {
            return Data.Value.GetTypeForMember(type, member);
        }

        public static AnimationValueRelationship GetAnimationValueRelationship(string frameMemberName)
        {
            return Data.Value.GetAnimationValueRelationship(frameMemberName);
        }

        public static string GetStateForVelocity(string velocity)
        {
            return Data.Value.GetStateForVelocity(velocity);
        }

        public static string GetRelativeForAbsolute(string absolute)
        {
            return Data.Value.GetRelativeForAbsolute(absolute);
        }

        public static string GetVelocityForState(string state)
        {
            return Data.Value.GetVelocityForState(state);

        }

        public static bool IsObjectReferencedByInstructions(object objectToReference)
        {
            return Data.Value.IsObjectReferencedByInstructions(objectToReference);
        }

        public static bool IsRotationMember(string rotationMember)
        {
            return Data.Value.IsRotationMember(rotationMember);
        }

        #region Move 

        public static void MoveThrough<T>(PositionedObject positionedObject, IList<T> list, float velocity) where T : IPositionable
        {
            Data.Value.MoveThrough(positionedObject, list, velocity);
        }


        public static void MoveToAccurate(FlatRedBall.PositionedObject positionedObject, float x, float y, float z, double secondsToTake)
        {
            Data.Value.MoveToAccurate(positionedObject,x, y, z, secondsToTake);
        }

        public static void MoveTo(PositionedObject positionedObject, float x, float y, float z, double secondsToTake)
        {
            Data.Value.MoveTo(positionedObject, x, y, z, secondsToTake);
        }

        #endregion

        #region Pause Methods

        internal static PositionedObjectList<PositionedObject> PositionedObjectsIgnoringPausing =>
            Data.Value.PositionedObjectsIgnoringPausing;

        public static List<object> ObjectsIgnoringPausing => Data.Value.ObjectsIgnoringPausing;

        public static void IgnorePausingFor(PositionedObject positionedObject)
        {
            Data.Value.IgnorePausingFor(positionedObject);
        }

        public static void IgnorePausingFor<T>(IList<T> list) where T : FlatRedBall.PositionedObject
        {
            Data.Value.IgnorePausingFor(list);
        }


        public static void IgnorePausingFor(Scene scene)
        {
            Data.Value.IgnorePausingFor(scene);
        }

        public static void IgnorePausingFor(ShapeCollection shapeCollection)
        {
            Data.Value.IgnorePausingFor(shapeCollection);
        }

        /// <summary>
        /// Pauses the instruction manager and prevents new instructions from being executed.
        /// </summary>
        public static void PauseEngine()
        {
            Data.Value.PauseEngine(true);
        }

        /// <summary>
        /// Pauses the instruction manager and prevents new instructions from being executed.
        /// </summary>
        /// <param name="storeUnpauseInstructions"></param>
        public static void PauseEngine(bool storeUnpauseInstructions)
        {
            Data.Value.PauseEngine(storeUnpauseInstructions);
        }

        /// <summary>
        /// Attempts to find an instruction to execute.
        /// </summary>
        /// <param name="target">Item to find and execute.</param>
        public static void FindAndExecuteUnpauseInstruction( object target )
        {
            Data.Value.FindAndExecuteUnpauseInstruction(target);
        }

        /// <summary>
        /// Unpauses the instruction manager and executes the tasks in the queue
        /// </summary>
        public static void UnpauseEngine()
        {
            Data.Value.UnpauseEngine();
        }

        #endregion

        /// <summary>
        /// Removes the argument instruction from the internal list. A removed instruction will not
        /// automatically be executed.
        /// </summary>
        /// <param name="instruction">The instruction to remove.</param>
        public static void Remove(Instruction instruction)
        {
            Data.Value.Remove(instruction);
        }


        /// <summary>
        /// Sets a member on an uncasted object.  If the type of objectToSetOn is known, use
        /// LateBinder for performance and safety reasons.
        /// </summary>
        /// <param name="objectToSetOn">The object whose field or property should be set.</param>
        /// <param name="memberName">The name of the field or property to set.</param>
        /// <param name="valueToSet">The value of the field or property to set.</param>
        public static void UncastedSetMember(object objectToSetOn, string memberName, object valueToSet)
        {
            Data.Value.UncastedSetMember(objectToSetOn, memberName, valueToSet);
        }

        #endregion

        #region Internal

        internal static IInterpolator GetInterpolator(Type type)
        {
            return Data.Value.GetInterpolator(type, InterpolatorType.Linear);
        }

        internal static IInterpolator GetInterpolator(Type type, string memberName)
        {
            return Data.Value.GetInterpolator(type, memberName);
        }

        internal static IInterpolator GetInterpolator(Type type, InterpolatorType interpolatorType)
        {
            return Data.Value.GetInterpolator(type, interpolatorType);
        }

        internal static bool HasInterpolatorForType(Type type)
        {
            return Data.Value.HasInterpolatorForType(type);
        }

        /// <summary>
        /// Performs every-frame updates which include moving queued instructions to the main instruction list and
        /// executing instructions according to their TimeToExecute.
        /// </summary>
        internal static void Update()
        {
            Data.Value.Update();
        }

        #endregion
        
        #endregion
    }
}
