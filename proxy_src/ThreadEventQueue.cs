using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FLOpenServerProxy
{
    public abstract class ReactorTimer
    {
        public double timeout;

        public double last_event_time;

        /// <summary>
        /// Set the timer to expire after timeout seconds.
        /// </summary>
        /// <param name="timeout">Seconds to expire.</param>
        public void ExpireAfter(double timeout)
        {
            this.timeout = timeout;
        }

        /// <summary>
        /// This method will be called when the timer expires
        /// </summary>
        public abstract void HandleTimerEvent(double delta_seconds);
    }

    public class ReactorEvent
    {
        /// <summary>
        /// This is the time the event was created. The average time from
        /// event creation to processing is the server load in seconds.
        /// </summary>
        public double event_start = Reactor.GetTime();
    }

    /// <summary>
    /// If this event is passed to the reactor, the main loop will
    /// exit once the event is dequeued.
    /// </summary>
    public class ReactorShutdownEvent : ReactorEvent
    {
    }


    public class Reactor
    {
        /// <summary>
        /// List of pending events.
        /// </summary>
        Queue<ReactorEvent> events = new Queue<ReactorEvent>();

        /// <summary>
        /// List of pending timers.
        /// </summary>
        List<ReactorTimer> timers = new List<ReactorTimer>();

        public void AddTimer(ReactorTimer timer)
        {
            timers.Add(timer);            
        }

        public void DelTimer(ReactorTimer timer)
        {
            timers.Remove(timer);
        }

        public void AddEvent(ReactorEvent new_event)
        {
            Monitor.Enter(events);
            events.Enqueue(new_event);
            Monitor.Pulse(events);
            Monitor.Exit(events);
        }

        /// <summary>
        /// Run timers and return the next event to process
        /// </summary>
        /// <returns></returns>
        public ReactorEvent Run(double current_time, double delta)
        {
            // By default go to sleep for half a second
            double shortest_timeout = 0.5;

            List<ReactorTimer> timers_copy = new List<ReactorTimer>(timers);
            foreach (ReactorTimer timer in timers_copy)
            {
                timer.timeout -= delta;
                // Call the timer event handler if needed.
                if (timer.timeout <= 0)
                {
                    if (timer.last_event_time == 0)
                        timer.last_event_time = current_time;

                    double timer_delta = current_time - timer.last_event_time;
                    timer.last_event_time = current_time;
                    timer.HandleTimerEvent(timer_delta);
                }
                // Check for timer expiry after calling handle event. The timer may have been
                // rescheduled in HandleTimerEvent and thus is no longer expired
                if (timer.timeout <= 0)
                    timers.Remove(timer);
                // Record when the next timeout is so we don't block too long in the
                // event queue
                else if (timer.timeout < shortest_timeout)
                    shortest_timeout = timer.timeout;
            }

            ReactorEvent next_event = GetNextEvent((int)(shortest_timeout * 1000));
            if (next_event != null)
            {
                double queue_time = GetTime() - next_event.event_start;
                average_queue_time += queue_time;
            }
            average_queue_time /= 2;
            return next_event;
        }

        double average_queue_time = 0;

        /// <summary>
        /// Return the current average of the processing time from an event being queued to
        /// it being unqueued at the receiving thread.
        /// </summary>
        /// <returns></returns>
        public double GetAverageQueueTime()
        {
            return average_queue_time;
        }

        /// <summary>
        /// Return the next event or null if there are no events and the
        /// timeout milliseconds have passed.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        private ReactorEvent GetNextEvent(int timeout)
        {
            if (timeout > 20)
                timeout = 20;

            ReactorEvent nextEvent = null;

            // If there's an event queued now, return the next immediately.
            Monitor.Enter(events);
            if (events.Count > 0)
                nextEvent = events.Dequeue();
            Monitor.Exit(events);

            if (nextEvent != null)
                return nextEvent;

            // If no timeout because lot's of pending timers, return immediately.
            if (timeout == 0)
                return null;

            // Wait for another event to be queued or for the timeout
            // to expire.
            Monitor.Enter(events);
            if (timeout > 0)
                Monitor.Wait(events, timeout);
            else
                Monitor.Wait(events);
            if (events.Count > 0)
                nextEvent = events.Dequeue();
            Monitor.Exit(events);
            
            return nextEvent;
        }

        /// <summary>
        /// Return the current time since system start in seconds.
        /// </summary>
        /// <returns>Time in seconds</returns>
        public static double GetTime()
        {
            double time = (System.Diagnostics.Stopwatch.GetTimestamp() / (double)System.Diagnostics.Stopwatch.Frequency);
            return time;
        }
    }
}
