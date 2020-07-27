using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace DeepU3.Timers
{
    /// <summary>
    /// Manages updating all the <see cref="Timer"/>s that are running in the application.
    /// This will be instantiated the first time you create a timer -- you do not need to add it into the
    /// scene manually.
    /// </summary>
    internal class TimerManager : MonoBehaviour
    {
        private HashSet<Timer> _timers = new HashSet<Timer>();

        // buffer adding timers so we don't edit a collection during iteration
        private HashSet<Timer> _timersToAdd = new HashSet<Timer>();

        public void RegisterTimer(Timer timer)
        {
            this._timersToAdd.Add(timer);
        }

        public void CancelAllTimers()
        {
            foreach (Timer timer in this._timers)
            {
                timer.Cancel();
            }

            this._timers = new HashSet<Timer>();
            this._timersToAdd = new HashSet<Timer>();
        }

        public void PauseAllTimers()
        {
            foreach (Timer timer in this._timers)
            {
                timer.Pause();
            }
        }

        public void ResumeAllTimers()
        {
            foreach (Timer timer in this._timers)
            {
                timer.Resume();
            }
        }

        // update all the registered timers on every frame
        [UsedImplicitly]
        private void Update()
        {
            this.UpdateAllTimers();
        }

        private void UpdateAllTimers()
        {
            if (this._timersToAdd.Count > 0)
            {
                foreach (var timer in this._timersToAdd)
                {
                    this._timers.Add(timer);
                }
                this._timersToAdd.Clear();
            }

            foreach (Timer timer in this._timers)
            {
                timer.Update();
            }

            this._timers.RemoveWhere(t => t.isDone);
        }
    }
}