using System.Collections.Generic;

using Microsoft.Xna.Framework;
using System.Text;

using System.Threading;
using FlatRedBall.IO;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FlatRedBall
{
    #region Enums
    /// <summary>
    /// Represents the unit of time measurement.  This can be used in files that store timing information.
    /// </summary>
    public enum TimeMeasurementUnit
    {
        Undefined,
        Millisecond,
        Second
    }
    #endregion

    #region Struct

    public struct TimedSection
    {
        public string Name;
        public double Time;

        public override string ToString() => $"{Name}: {Time}";
    }

    internal struct TimedTasks
    {
        public double Time;
        public TaskCompletionSource<object> TaskCompletionSource;
    }

    internal struct PredicateTask
    {
        public Func<bool> Predicate;
        public TaskCompletionSource<object> TaskCompletionSource;
    }

    internal struct FrameTask
    {
        public int FrameIndex;
        public TaskCompletionSource<object> TaskCompletionSource;
    }

    #endregion
    
    /// <summary>
    /// Class providing timing information for the current frame, absolute time since the game has started running, and for the current screen.
    /// </summary>
    public static class TimeManager
    {
        #region Classes

        struct VoidTaskResult { }



        #endregion

        #region Fields

        /// <summary>
        /// The amount of time in seconds since the game started running. 
        /// This value is updated once-per-frame so it will 
        /// always be the same value until the next frame is called.
        /// This value does not consider pausing. To consider pausing, see CurrentScreenTime.
        /// </summary>
        /// <remarks>
        /// This value can be used to uniquely identify a frame.
        /// </remarks>
        public static double CurrentTime;
        public static int CurrentFrame;

        private static double _mCurrentTimeForTimedSections;

        private static readonly Lazy<System.Diagnostics.Stopwatch> StopWatch = new Lazy<Stopwatch>(() =>
        {
            _stringBuilder = new StringBuilder(200);
            // This may be initialized outside of FRB if the user is trying to time pre-FRB calls
            var newStopWatch = new System.Diagnostics.Stopwatch();
            newStopWatch.Start();
            return newStopWatch;
        });

        private static readonly List<double> Sections = new List<double>();
        private static readonly List<string> SectionLabels = new List<string>();

        private static readonly List<double> LastSections = new List<double>();
        private static readonly List<string> LastSectionLabels = new List<string>();

        private static readonly Dictionary<string, double> MPersistentSections = new Dictionary<string, double>();
        private static double _mLastPersistentTime;

        private static readonly Dictionary<string, double> MSumSections = new Dictionary<string, double>();
        private static double _mLastSumTime;

        private static StringBuilder _stringBuilder;

        private static bool _mIsPersistentTiming = false;

        private static readonly List<TimedTasks> ScreenTimeDelayedTasks = new List<TimedTasks>();
        private static readonly List<PredicateTask> PredicateTasks = new List<PredicateTask>();
        private static readonly List<FrameTask> FrameTasks = new List<FrameTask>();

        #endregion

        #region Properties

        public static double LastCurrentTime { get; private set; }

        /// <summary>
        /// The number of seconds (usually a fraction of a second) since
        /// the last frame.  This value can be used for time-based movement.
        /// This value is changed once per frame, and will remain constant within each frame, assuming a consant TimeFactor.
        /// Changing the TimeFactor adjusts this value.
        /// </summary>
        public static float SecondDifference { get; private set; }

        public static float LastSecondDifference { get; private set; }

        public static float SecondDifferenceSquaredDividedByTwo { get; private set; }

        public static bool TimeSectionsEnabled { get; set; } = true;

        /// <summary>
        /// A multiplier for how fast time runs.  This is 1 by default.  Setting
        /// this value to 2 will make everything run twice as fast. Increasing this value
        /// effectively increases the SecondDifference value, so custom code which is time-based
        /// will behave properly when TimeFactor is adjusted.
        /// </summary>
        public static double TimeFactor { get; set; } = 1.0f;

        public static GameTime LastUpdateGameTime { get; private set; }

        public static TimeMeasurementUnit TimedSectionReportingUnit { get; set; } = TimeMeasurementUnit.Millisecond;

        public static float MaxFrameTime { get; set; } = 0.5f;

        /// <summary>
        /// Returns the amount of time since the current screen started. This value does not 
        /// advance when the screen is paused.
        /// </summary>
        /// <remarks>
        /// This value is the same as 
        /// Screens.ScreenManager.CurrentScreen.PauseAdjustedCurrentTime
        /// </remarks>
        public static double CurrentScreenTime => Screens.ScreenManager.CurrentScreen.PauseAdjustedCurrentTime;

        [Obsolete("Use CurrentSystemTime as that name is more consistent and this will eventually be removed.")]
        public static double SystemCurrentTime => StopWatch.Value.Elapsed.TotalSeconds;

        public static double CurrentSystemTime => StopWatch.Value.Elapsed.TotalSeconds; 


        public static int TimedSectionCount => Sections.Count;

        public static bool SetNextFrameTimeTo0 { get; set; }

        #endregion

        #region Methods

        public static void CreateXmlSumTimeSectionReport(string fileName)
        {
            var tempList = GetTimedSectionList();

            FileManager.XmlSerialize(tempList, fileName);
        }

        public static List<TimedSection> GetTimedSectionList()
        {
           var tempList = new List<TimedSection>(MSumSections.Count);
           tempList.AddRange(MSumSections.Select(kvp => new TimedSection() { Name = kvp.Key, Time = kvp.Value }));

           return tempList;
        }


        #region TimeSection code

        public static string GetPersistentTimedSections()
        {

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (KeyValuePair<string, double> kvp in MPersistentSections)
            {
                sb.Append(kvp.Key).Append(": ").AppendLine(kvp.Value.ToString());
            }

            _mIsPersistentTiming = false;

            return sb.ToString();
        }


        public static string GetSumTimedSections()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (KeyValuePair<string, double> kvp in MSumSections)
            {
				if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
				{
					sb.Append(kvp.Key).Append(": ").AppendLine((kvp.Value * 1000.0f).ToString("f2"));
				}
				else
				{
					sb.Append(kvp.Key).Append(": ").AppendLine(kvp.Value.ToString());
				}
            }

            return sb.ToString();
        }


        public static string GetTimedSections(bool showTotal)
        {
            _stringBuilder.Remove(0, _stringBuilder.Length);

            int largestIndex = -1;
            double longestTime = -1;

            for (int i = 0; i < LastSections.Count; i++)
            {
                if (LastSections[i] > longestTime)
                {
                    longestTime = LastSections[i];
                    largestIndex = i;
                }
            }

            for (int i = 0; i < LastSections.Count; i++)
            {
                if (i == largestIndex)
                {
					if (LastSectionLabels[i] != "")
					{
						if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
						{
							_stringBuilder.Append("-!-" + LastSectionLabels[i]).Append(": ").Append(LastSections[i].ToString("f2")).Append("\n");
						}
						else
						{
							_stringBuilder.Append("-!-" + LastSectionLabels[i]).Append(": ").Append(LastSections[i].ToString()).Append("\n");
						}
					}
					else
					{
						if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
						{
							_stringBuilder.Append("-!-" + LastSections[i].ToString("f2")).Append("\n");
						}
						else
						{
							_stringBuilder.Append("-!-" + LastSections[i].ToString()).Append("\n");
						}
					}
                }
                else
                {
					if (LastSectionLabels[i] != "")
					{
						if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
						{
							_stringBuilder.Append(LastSectionLabels[i]).Append(": ").Append(LastSections[i].ToString("f2")).Append("\n");
						}
						else
						{
							_stringBuilder.Append(LastSectionLabels[i]).Append(": ").Append(LastSections[i].ToString()).Append("\n");
						}
					}
					else
					{
						if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
						{
							_stringBuilder.Append(LastSections[i].ToString("f2")).Append("\n");
						}
						else
						{
							_stringBuilder.Append(LastSections[i].ToString()).Append("\n");
						}
					}
                }
            }

            if (showTotal)
			{
				if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
				{
					_stringBuilder.Append("Total Timed: " + ((TimeManager.CurrentSystemTime - TimeManager._mCurrentTimeForTimedSections) * 1000.0f).ToString("f2"));
				}
				else
				{
					_stringBuilder.Append("Total Timed: " + (TimeManager.CurrentSystemTime - TimeManager._mCurrentTimeForTimedSections));
				}
			}

            return _stringBuilder.ToString();

        }


        public static void PersistentTimeSection(string label)
        {
            if (_mIsPersistentTiming)
            {
                double currentTime = CurrentSystemTime;
                if (MPersistentSections.ContainsKey(label))
                {
					if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
					{
						MPersistentSections[label] = ((currentTime - _mLastPersistentTime) * 1000.0f);
					}
					else
					{
						MPersistentSections[label] = currentTime - _mLastPersistentTime;
					}
                }
                else
                {
					if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
					{
						MPersistentSections.Add(label, (currentTime - _mLastPersistentTime) * 1000.0f);
					}
					else
					{
						MPersistentSections.Add(label, currentTime - _mLastPersistentTime);
					}
                }

                _mLastPersistentTime = currentTime;
            }
        }



        public static void StartPersistentTiming()
        {
            MPersistentSections.Clear();

            _mIsPersistentTiming = true;

            _mLastPersistentTime = CurrentSystemTime;
        }

        /// <summary>
        /// Begins Sum Timing
        /// </summary>
        /// <remarks>
        /// <code>
        /// 
        /// StartSumTiming();
        /// 
        /// foreach(Sprite sprite in someSpriteArray)
        /// {
        ///     SumTimeRefresh();
        ///     PerformSomeFunction(sprite);
        ///     SumTimeSection("PerformSomeFunction time:");
        /// 
        /// 
        ///     SumTimeRefresh();
        ///     PerformSomeOtherFunction(sprite);
        ///     SumTimeSection("PerformSomeOtherFunction time:);
        /// 
        /// }
        /// </code>
        ///
        /// </remarks>
        public static void StartSumTiming()
        {
            MSumSections.Clear();

            _mLastSumTime = CurrentSystemTime;
        }


        public static void SumTimeSection(string label)
        {
            double currentTime = CurrentSystemTime;
            if (MSumSections.ContainsKey(label))
            {
                MSumSections[label] += currentTime - _mLastSumTime;
                //mSumSectionHitCount[label]++;
            }
            else
            {
                MSumSections.Add(label, currentTime - _mLastSumTime);
                //mSumSectionHitCount.Add(label, 1);
            }
            _mLastSumTime = currentTime;
        }


        public static void SumTimeRefresh()
        {
            _mLastSumTime = CurrentSystemTime;
        }

        /// <summary>
        /// Stores an unnamed timed section.
        /// </summary>
        /// <remarks>
        /// A timed section is the amount of time (in seconds) since the last time either Update
        /// or TimeSection has been called.  The sections are reset every time Update is called.
        /// The sections can be retrieved through the GetTimedSections method.
        /// <seealso cref="FRB.TimeManager.GetTimedSection"/>
        /// </remarks>
        public static void TimeSection()
        {
            TimeSection("");
        }


        /// <summary>
        /// Stores an named timed section.
        /// </summary>
        /// <remarks>
        /// A timed section is the amount of time (in seconds) since the last time either Update
        /// or TimeSection has been called.  The sections are reset every time Update is called.
        /// The sections can be retrieved through the GetTimedSections method.
        /// <seealso cref="FRB.TimeManager.GetTimedSection"/>
        /// </remarks>
        /// <param name="label">The label for the timed section.</param>
        public static void TimeSection(string label)
        {
            if (TimeSectionsEnabled)
            {
                Monitor.Enter(Sections);

                double f = (CurrentSystemTime - _mCurrentTimeForTimedSections);
                if (TimedSectionReportingUnit == TimeMeasurementUnit.Millisecond)
                {
                    f *= 1000.0f;
                }

                for (int i = Sections.Count - 1; i > -1; i--)
                    f -= Sections[i];


                Sections.Add(f);
                SectionLabels.Add(label);

                Monitor.Exit(Sections);
            }
        }

        #endregion

        /// <summary>
        /// Returns the number of seconds which have passed since the argument value in game time.
        /// This value continues to increment when the screen is paused, and does not reset when switching screens.
        /// Usually game logic should use CurrentScreenSecondsSince.
        /// </summary>
        /// <remarks>
        /// This value will only change once per frame, so it can be called multiple times per frame and the same
        /// value will be returned, assuming the same parameter is passed.
        /// </remarks>
        /// <param name="absoluteTime">The amount of time since the start of the game.</param>
        /// <returns>The number of seconds which have passed in absolute time since the start of the game.</returns>
        public static double SecondsSince(double absoluteTime)
        {
            return CurrentTime - absoluteTime;
        }

        /// <summary>
        /// Returns the number of seconds that have passed since the arugment value. The
        /// return value will not increase when the screen is paused, so it can be used to 
        /// determine how much game time has passed for event swhich should occur on a timer.
        /// </summary>
        /// <param name="time">The time value, probably obtained earlier by calling CurrentScreenTime</param>
        /// <returns>The number of unpaused seconds that have passed since the argument time.</returns>
        public static double CurrentScreenSecondsSince(double time)
        {
            return Screens.ScreenManager.CurrentScreen.PauseAdjustedSecondsSince(time);
        }

        public static Task Delay(TimeSpan timeSpan)
        {
            return DelaySeconds(timeSpan.TotalSeconds);
        }

        public static Task DelaySeconds(double seconds)
        {
            if(seconds <= 0)
            {
                return Task.CompletedTask;
            }
            var time = CurrentScreenTime + seconds;
            var taskSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var index = ScreenTimeDelayedTasks.Count;
            for(var i = 0; i < ScreenTimeDelayedTasks.Count; i++)
            {
                if (ScreenTimeDelayedTasks[i].Time > time)
                {
                    index = i;
                    break;
                }
            }

            ScreenTimeDelayedTasks.Insert(index, new TimedTasks { Time = time, TaskCompletionSource = taskSource});

            return taskSource.Task;
        }

        public static Task DelayUntil(Func<bool> predicate)
        {
            if(predicate())
            {
                return Task.CompletedTask;
            }
            var taskSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            PredicateTasks.Add(new PredicateTask { Predicate = predicate, TaskCompletionSource = taskSource });
            return taskSource.Task;
        }

        public static Task DelayFrames(int frameCount)
        {
            if(frameCount <= 0)
            {
                return Task.CompletedTask;
            }
            var taskSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var index = FrameTasks.Count;
            var absoluteFrame = TimeManager.CurrentFrame + frameCount;
            for (var i = 0; i < FrameTasks.Count; i++)
            {
                if (FrameTasks[i].FrameIndex > absoluteFrame)
                {
                    index = i;
                    break;
                }
            }
            FrameTasks.Insert(index, new FrameTask { FrameIndex = absoluteFrame, TaskCompletionSource = taskSource });
            return taskSource.Task;

        }

        private static bool _isFirstUpdate = false;
        /// <summary>
        /// Performs every-frame logic to update timing values such as CurrentTime and SecondDifference.  If this method is not called, CurrentTime will not advance.
        /// </summary>
        /// <param name="time">The GameTime value provided by the MonoGame Game class.</param>
        public static void Update(GameTime time)
        {
            LastUpdateGameTime = time;

            LastSections.Clear();
            LastSectionLabels.Clear();

            for (int i = Sections.Count - 1; i > -1; i--)
            {

                LastSections.Insert(0, Sections[i]);
                LastSectionLabels.Insert(0, SectionLabels[i]);
            }

            Sections.Clear();
            SectionLabels.Clear();

            LastSecondDifference = SecondDifference;
            LastCurrentTime = CurrentTime;

            const bool useSystemCurrentTime = false;

            double elapsedTime;

            // This exists if we ever add a platform back in (such as how MDX used to be)
            // which does not have a built-in Game timing mechanism the way XNA does
            if (useSystemCurrentTime)
            {
                double systemCurrentTime = CurrentSystemTime;
                elapsedTime = systemCurrentTime - LastCurrentTime;
                LastCurrentTime = systemCurrentTime;
                //stop big frame times
                if (elapsedTime > MaxFrameTime)
                {
                    elapsedTime = MaxFrameTime;
                }
            }
            else
            {
                /*
                mSecondDifference = (float)(currentSystemTime - mCurrentTime);
                mCurrentTime = currentSystemTime;
                */

                if (SetNextFrameTimeTo0)
                {
                    elapsedTime = 0;
                    SetNextFrameTimeTo0 = false;
                }
                else
                {
                    elapsedTime = time.ElapsedGameTime.TotalSeconds * TimeFactor;
                }

                //stop big frame times
                if (elapsedTime > MaxFrameTime)
                {
                    elapsedTime = MaxFrameTime;
                }
            }

            SecondDifference = (float)(elapsedTime);
            CurrentTime += elapsedTime;

            double currentSystemTime = CurrentSystemTime + SecondDifference;

            SecondDifferenceSquaredDividedByTwo = (SecondDifference * SecondDifference) / 2.0f;
            _mCurrentTimeForTimedSections = currentSystemTime;

            if (_isFirstUpdate)
            {
                _isFirstUpdate = false;
            }
            else
            {
                CurrentFrame++;
            }
        }

        internal static void DoTaskLogic()
        {

            // Check if any delayed tasks should be completed
            while (ScreenTimeDelayedTasks.Count > 0)
            {
                var first = ScreenTimeDelayedTasks[0];
                if (first.Time <= CurrentScreenTime)
                {
                    ScreenTimeDelayedTasks.RemoveAt(0);
                    first.TaskCompletionSource.SetResult(null);
                }
                else
                {
                    // The earliest task is not ready to be completed, so we can stop checking
                    break;
                }
            }

            // Check if any predicate tasks should be completed
            // do a reverse loop, run the predicate, and remove them and set their result to null if the predicate is true
            for(var i = PredicateTasks.Count - 1; i > -1; i--)
            {
                var predicateTask = PredicateTasks[i];
                if(predicateTask.Predicate())
                {
                    PredicateTasks.RemoveAt(i);
                    predicateTask.TaskCompletionSource.SetResult(null);
                }
            }

            while(FrameTasks.Count > 0)
            {
                var first = FrameTasks[0];
                if(first.FrameIndex <= CurrentFrame)
                {
                    FrameTasks.RemoveAt(0);
                    first.TaskCompletionSource.SetResult(null);
                }
                else
                {
                    break;
                }
            }

        }

        internal static void ClearTasks()
        {
            foreach (var timedTasks in ScreenTimeDelayedTasks.ToList())
            {
                timedTasks.TaskCompletionSource.SetCanceled();
            }
            ScreenTimeDelayedTasks.Clear();

            foreach(var predicateTask in PredicateTasks.ToList())
            {
                predicateTask.TaskCompletionSource.SetCanceled();
            }   
            PredicateTasks.Clear();

            foreach(var frameTask in FrameTasks.ToList())
            {
                frameTask.TaskCompletionSource.SetCanceled();
            }
            FrameTasks.Clear();
        }

        #endregion
    }
}


