using System;
using System.Collections.Generic;
using FOS_ng.Data.Arch;
using Jitter.LinearMath;

namespace FOS_ng.Objects
{
    public abstract class SimObject : IUpdatable
    {

        /// <summary>
        /// Bucket of objects close to this SimObject - less than 1000M.
        /// TODO: use that
        /// </summary>
        public List<Tuple<float, SimObject>> Bucket = new List<Tuple<float, SimObject>>();

        /// <summary>
        /// Bucket of objects close to this SimObject - 10000M or in scanner range.
        /// TODO: use that
        /// </summary>
        public List<SimObject> ScanBucket = new List<SimObject>();

        protected uint BucketRange = 5000;

        private readonly bool _stationary;
        private bool _bucketInitialized;
        /// <summary>
        ///     The archetype id of the object.
        /// </summary>
        public Archetype Arch;

        /// <summary>
        ///     The health of this object, from 0 to 1.
        /// </summary>
        public float Health;

        /// <summary>
        ///     The ID of this object in space.
        /// </summary>
        public uint Objid;

        /// <summary>
        ///     The current orientation of this object.
        /// </summary>
        public JMatrix Orientation = new JMatrix();

        /// <summary>
        ///     The current position of this object
        /// </summary>
        public JVector Position = new JVector();


        /// <summary>
        ///     The current throttle position of this object.
        /// </summary>
        public float Throttle;

        /// <summary>
        ///     This is the time of the last position update to this object
        ///     since the object was created.
        /// </summary>
        public double UpdateTime;

        public Universe.System System;

        /// <summary>
        ///     Create the simulation object. Sometimes this object is used for data
        ///     storage rather than a live simulation object. This these cases,  the
        ///     runner can be null.
        /// </summary>
        protected SimObject(Universe.System system)
        {
            System = system;
            //TODO: make timer tweaks
            //ExpireAfter(0.1);

            //TODO: check dis
             _stationary = (this is Solar.Solar);
        }

        /// <summary>
        ///     This method is called appromiately every 100ms. Inheriting classes should implement
        ///     movement in this.
        /// </summary>
        /// <param name="deltaSeconds">The time in seconds since the last update</param>
        public virtual bool Update(float deltaSeconds)
        {
            return true;
        }

        /// <summary>
        /// Updates bucket of near objects.
        /// </summary>
        private void UpdateBucket()
        {

            if (!_stationary)
            {
                Bucket.Clear();
                ScanBucket.Clear();
                foreach (var so in System.Objects)
                {
                    var pos = (Position - so.Value.Position).Length();
                    if (pos < 1000) Bucket.Add(Tuple.Create(pos, so.Value));
                    if (pos < BucketRange) ScanBucket.Add(so.Value);
                }
            }
            else
            {

                if (!_bucketInitialized)
                {
                    foreach (var so in System.Objects)
                    {
                        if (!(so.Value is Solar.Solar)) continue;
                        var pos = (Position - so.Value.Position).Length();
                        if (pos < 1000) Bucket.Add(Tuple.Create(pos, so.Value));
                        if (pos < BucketRange) ScanBucket.Add(so.Value);
                    }
                    _bucketInitialized = true;
                }

                Bucket.RemoveAll(v => !(v.Item2 is Solar.Solar));
                ScanBucket.RemoveAll(v => !(v is Solar.Solar));

                foreach (var so in System.AffObjects)
                {
                    var pos = (Position - so.Value.Position).Length();
                    if (pos < 1000) Bucket.Add(Tuple.Create(pos, so.Value));
                    if (pos < BucketRange) ScanBucket.Add(so.Value);
                }
            }

                
            
        }

        /// <summary>
        ///     Return the actual or estimated position of this simobject.
        /// </summary>
        /// <returns></returns>
        public abstract JVector ExtrapolatedPosition();

        /// <summary>
        /// 
        /// </summary>
        public virtual bool LongUpdate(float deltaSeconds)
        {
            
            if (System != null)
            {
                //TODO: CPU hog.
                UpdateBucket();
                //UpdateFarBucket();
            }
            return true;
        }


    }
}