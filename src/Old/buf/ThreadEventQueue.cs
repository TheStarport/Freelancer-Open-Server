using System.Collections.Generic;
using System.Threading;

namespace FLServer.buf
{
    public abstract class ReactorTimer
    {
        public double LastEventTime;
        public volatile Reactor Owner;

        public double Timeout;

        /// <summary>
        ///     Set the timer to expire after timeout seconds.
        /// </summary>
        /// <param name="timeout">Seconds to expire.</param>
        public void ExpireAfter(double timeout)
        {
            Timeout = timeout;
        }

        /// <summary>
        ///     This method will be called when the timer expires
        /// </summary>
        public abstract void HandleTimerEvent(double deltaSeconds);
    }

    public class ReactorEvent
    {
        /// <summary>
        ///     This is the time the event was created. The average time from
        ///     event creation to processing is the server load in seconds.
        /// </summary>
        public double EventStart = Utilities.GetTime();
    }

    /// <summary>
    ///     If this event is passed to the reactor, the main loop will
    ///     exit once the event is dequeued.
    /// </summary>
    public class ReactorShutdownEvent : ReactorEvent
    {
    }


    public class Reactor
    {
        /// <summary>
        ///     List of pending events.
        /// </summary>
        private readonly Queue<ReactorEvent> _events = new Queue<ReactorEvent>();

        /// <summary>
        ///     List of pending timers.
        /// </summary>
        private readonly List<ReactorTimer> _timers = new List<ReactorTimer>();

        private double _averageQueueTime;

        public void AddTimer(ReactorTimer timer)
        {
            timer.Owner = this;
            _timers.Add(timer);
        }

        public void DelTimer(ReactorTimer timer)
        {
            _timers.Remove(timer);
        }

        public void AddEvent(ReactorEvent newEvent)
        {
            Monitor.Enter(_events);
            _events.Enqueue(newEvent);
            Monitor.Pulse(_events);
            Monitor.Exit(_events);
        }

        /// <summary>
        ///     Run timers and return the next event to process
        /// </summary>
        /// <returns></returns>
        public ReactorEvent Run(double currentTime, double delta)
        {
            // By default go to sleep for half a second
            var shortestTimeout = 0.1;

            var timersCopy = new List<ReactorTimer>(_timers);
            foreach (ReactorTimer timer in timersCopy)
            {
                if (timer.Owner != this)
                {
                    _timers.Remove(timer);
                    continue;
                }

                timer.Timeout -= delta;
                // Call the timer event handler if needed.
                if (timer.Timeout <= 0)
                {
                    if (timer.LastEventTime == 0)
                        timer.LastEventTime = currentTime;

                    var timerDelta = currentTime - timer.LastEventTime;
                    timer.LastEventTime = currentTime;
                    timer.HandleTimerEvent(timerDelta);
                }
                // Check for timer expiry after calling handle event. The timer may have been
                // rescheduled in HandleTimerEvent and thus is no longer expired
                if (timer.Timeout <= 0)
                {
                    _timers.Remove(timer);
                    continue;
                }

                // Record when the next timeout is so we don't block too long in the
                // event queue
                if (timer.Timeout < shortestTimeout)
                {
                    shortestTimeout = timer.Timeout;
                }
            }

            var nextEvent = GetNextEvent((int) (shortestTimeout*1000));
            if (nextEvent != null)
            {
                var queueTime = Utilities.GetTime() - nextEvent.EventStart;
                _averageQueueTime += queueTime;
            }
            _averageQueueTime /= 2;
            return nextEvent;
        }

        /// <summary>
        ///     Return the current average of the processing time from an event being queued to
        ///     it being unqueued at the receiving thread.
        /// </summary>
        /// <returns></returns>
        public double GetAverageQueueTime()
        {
            return _averageQueueTime;
        }

        /// <summary>
        ///     Return the next event or null if there are no events and the
        ///     timeout milliseconds have passed.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        private ReactorEvent GetNextEvent(int timeout)
        {
            if (timeout > 20)
                timeout = 20;

            ReactorEvent nextEvent = null;

            // If there's an event queued now, return the next immediately.
            Monitor.Enter(_events);
            if (_events.Count > 0)
                nextEvent = _events.Dequeue();
            Monitor.Exit(_events);

            if (nextEvent != null)
                return nextEvent;

            // If no timeout because lot's of pending timers, return immediately.
            if (timeout == 0)
                return null;

            // Wait for another event to be queued or for the timeout
            // to expire.
            Monitor.Enter(_events);
            if (timeout > 0)
                Monitor.Wait(_events, timeout);
            else
                Monitor.Wait(_events);
            if (_events.Count > 0)
                nextEvent = _events.Dequeue();
            Monitor.Exit(_events);

            return nextEvent;
        }
    }
}