using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Security.Permissions;

namespace NBear.Common.Caching
{
    /// <summary>
    /// The Cache class is the traffic cop that prevents 
    /// resource contention among the different threads in the system. 
    /// </summary>	
    public class Cache
    {
        private Hashtable inMemoryCache = Hashtable.Synchronized(new Hashtable());

        /// <summary>
        /// Gets the count of cached objects.
        /// </summary>
        /// <value>
        /// The count of cached objects.
        /// </value>
        public int Count
        {
            get { return inMemoryCache.Count; }
        }

        /// <summary>
        /// Adds cache with specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The obj.</param>
        public void Add(string key, object value)
        {
            Add(key, value, null);
        }

        /// <summary>
        /// Adds cache with specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The obj.</param>
        /// <param name="expirations">The expiration strategy.</param>
        public void Add(string key, object value, params ICacheItemExpiration[] expirations)
        {
            ValidateKey(key);

            CacheItem cacheItemBeforeLock = null;


            lock (inMemoryCache.SyncRoot)
            {
                if (!inMemoryCache.Contains(key))
                {
                    cacheItemBeforeLock = new CacheItem(key, value, expirations);
                    inMemoryCache[key] = cacheItemBeforeLock;
                }
                else
                {
                    cacheItemBeforeLock = (CacheItem)inMemoryCache[key];
                    try
                    {
                        cacheItemBeforeLock.Replace(value, expirations);
                        inMemoryCache[key] = cacheItemBeforeLock;
                    }
                    catch
                    {
                        inMemoryCache.Remove(key);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Removes cached object with specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        public void Remove(string key)
        {
            ValidateKey(key);

            lock (inMemoryCache.SyncRoot)
            {
                if (inMemoryCache.ContainsKey(key))
                {
                    inMemoryCache.Remove(key);
                }
            }
        }

        /// <summary>
        /// Remove cached objects with specified key prefix.
        /// </summary>
        /// <param name="keyPrefix">The key prefix.</param>
        public void RemoveByKeyPrefix(string keyPrefix)
        {
            lock (inMemoryCache)
            {
                List<string> toRemoveKeys = new List<string>();
                foreach (string key in inMemoryCache.Keys)
                {
                    if (key.StartsWith(keyPrefix))
                    {
                        toRemoveKeys.Add(key);
                    }
                }

                foreach (string key in toRemoveKeys)
                {
                    inMemoryCache.Remove(key);
                }
            }
        }

        /// <summary>
        /// Gets cached obj with specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>the obj</returns>
        public object Get(string key)
        {
            ValidateKey(key);

            CacheItem cacheItem = (CacheItem)inMemoryCache[key];

            if (cacheItem == null)
            {
                return null;
            }

            lock (inMemoryCache.SyncRoot)
            {
                if (cacheItem.HasExpired())
                {
                    cacheItem.TouchedByUserAction(true);

                    inMemoryCache.Remove(key);

                    return null;
                }
            }

            cacheItem.TouchedByUserAction(false);

            return cacheItem.Value;
        }

        /// <summary>
        /// Flush the cache.
        /// </summary>
        public void Clear()
        {
        RestartFlushAlgorithm:
            lock (inMemoryCache.SyncRoot)
            {
                foreach (string key in inMemoryCache.Keys)
                {
                    bool lockWasSuccessful = false;
                    CacheItem itemToRemove = (CacheItem)inMemoryCache[key];
                    try
                    {
                        if (lockWasSuccessful = Monitor.TryEnter(itemToRemove))
                        {
                            itemToRemove.TouchedByUserAction(true);
                        }
                        else
                        {
                            goto RestartFlushAlgorithm;
                        }
                    }
                    finally
                    {
                        if (lockWasSuccessful) Monitor.Exit(itemToRemove);
                    }
                }

                inMemoryCache.Clear();
            }
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("DEFAULT_KEY could not be null.");
            }
        }
    }

    /// <summary>
    /// This class contains all data important to define an item stored in the cache. It holds both the DEFAULT_KEY and 
    /// value specified by the user, as well as housekeeping information used internally by this block. It is public, 
    /// rather than internal, to allow block extenders access to it inside their own implementations of IBackingStore.
    /// </summary>
    public class CacheItem
    {
        // User-provided data
        private string key;
        private object data;

        private ICacheItemExpiration[] expirations;
        private bool willBeExpired;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="expirations">The expirations.</param>
        public CacheItem(string key, object value, params ICacheItemExpiration[] expirations)
        {
            Initialize(key, value, expirations);

            TouchedByUserAction(false);
        }

        /// <summary>
        /// Replaces the internals of the current cache item with the given new values. This is strictly used in the Cache
        /// class when adding a new item into the cache. By replacing the item's contents, rather than replacing the item
        /// itself, it allows us to keep a single reference in the cache, simplifying locking.
        /// </summary>
        /// <param name="cacheItemData">Value to be stored. May be null.</param>
        /// <param name="cacheItemExpirations">Param array of ICacheItemExpiration objects. May provide 0 or more of these.</param>
        internal void Replace(object cacheItemData, params ICacheItemExpiration[] cacheItemExpirations)
        {
            Initialize(this.key, cacheItemData, cacheItemExpirations);
            TouchedByUserAction(false);
        }

        /// <summary>
        /// Intended to be used internally only. The value should be true when an item is eligible to be expired.
        /// </summary>
        public bool WillBeExpired
        {
            get { return willBeExpired; }
            set { willBeExpired = value; }
        }

        /// <summary>
        /// Returns the cached value of this CacheItem
        /// </summary>
        public object Value
        {
            get { return data; }
        }

        /// <summary>
        /// Returns the DEFAULT_KEY associated with this CacheItem
        /// </summary>
        public string Key
        {
            get { return key; }
        }

        /// <summary>
        /// Returns array of <see cref="ICacheItemExpiration"/> objects for this instance.
        /// </summary>
        /// <returns>
        /// An array of <see cref="ICacheItemExpiration"/> objects.
        /// </returns>
        public ICacheItemExpiration[] GetExpirations()
        {
            return (ICacheItemExpiration[])expirations.Clone();
        }

        /// <summary>
        /// Evaluates all cacheItemExpirations associated with this cache item to determine if it 
        /// should be considered expired. Evaluation stops as soon as any expiration returns true. 
        /// </summary>
        /// <returns>True if item should be considered expired, according to policies
        /// defined in this item's cacheItemExpirations.</returns>
        public bool HasExpired()
        {
            foreach (ICacheItemExpiration expiration in expirations)
            {
                if (expiration.HasExpired())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Intended to be used internally only. This method is called whenever a CacheItem is touched through the action of a user. It
        /// prevents this CacheItem from being expired or scavenged during an in-progress expiration or scavenging process. It has no effect
        /// on subsequent expiration or scavenging processes.
        /// </summary>
        public void TouchedByUserAction(bool objectRemovedFromCache)
        {
            TouchedByUserAction(objectRemovedFromCache, DateTime.Now);
        }

        /// <summary>
        /// Intended to be used internally only. This method is called whenever a CacheItem is touched through the action of a user. It
        /// prevents this CacheItem from being expired or scavenged during an in-progress expiration or scavenging process. It has no effect
        /// on subsequent expiration or scavenging processes.
        /// </summary>
        internal void TouchedByUserAction(bool objectRemovedFromCache, DateTime timestamp)
        {
            willBeExpired = objectRemovedFromCache ? false : HasExpired();
        }

        private void InitializeExpirations()
        {
            foreach (ICacheItemExpiration expiration in expirations)
            {
                expiration.Initialize(this);
            }
        }

        private void Initialize(string cacheItemKey, object cacheItemData, ICacheItemExpiration[] cacheItemExpirations)
        {
            key = cacheItemKey;
            data = cacheItemData;
            if (cacheItemExpirations == null)
            {
                expirations = new ICacheItemExpiration[1] { new NeverExpired() };
            }
            else
            {
                expirations = cacheItemExpirations;
            }
        }
    }

    /// <summary>
    ///	Allows end users to implement their own cache item expiration schema.
    /// </summary>
    public interface ICacheItemExpiration
    {
        /// <summary>
        ///	Specifies if item has expired or not.
        /// </summary>
        /// <returns>Returns true if the item has expired, otherwise false.</returns>
        bool HasExpired();

        /// <summary>
        /// Called to give the instance the opportunity to initialize itself from information contained in the CacheItem.
        /// </summary>
        /// <param name="owningCacheItem">CacheItem that owns this expiration object</param>
        void Initialize(CacheItem owningCacheItem);
    }

    /// <summary>
    /// This class reflects an expiration policy of never being expired.
    /// </summary>
    [Serializable]
    public class NeverExpired : ICacheItemExpiration
    {
        /// <summary>
        /// Always returns false
        /// </summary>
        /// <returns>False always</returns>
        public bool HasExpired()
        {
            return false;
        }

        /// <summary>
        /// Not used
        /// </summary>
        /// <param name="owningCacheItem">Not used</param>
        public void Initialize(CacheItem owningCacheItem)
        {
        }
    }

    /// <summary>
    ///	This class tests if a data item was expired using a absolute time 
    ///	schema.
    /// </summary>
    [Serializable]
    public class AbsoluteTime : ICacheItemExpiration
    {
        private DateTime absoluteExpirationTime;

        /// <summary>
        ///	Create an instance of the class with a time value as input and 
        ///	convert it to UTC.
        /// </summary>
        /// <param name="absoluteTime">
        ///	The time to be checked for expiration
        /// </param>
        public AbsoluteTime(DateTime absoluteTime)
        {
            if (absoluteTime > DateTime.Now)
            {
                // Convert to UTC in order to compensate for time zones	
                this.absoluteExpirationTime = absoluteTime.ToUniversalTime();
            }
            else
            {
                throw new ArgumentOutOfRangeException("absoluteTime out of range");
            }
        }

        /// <summary>
        /// Gets the absolute expiration time.
        /// </summary>
        /// <value>
        /// The absolute expiration time.
        /// </value>
        public DateTime AbsoluteExpirationTime
        {
            get { return absoluteExpirationTime; }
        }

        /// <summary>
        /// Creates an instance based on a time interval starting from now.
        /// </summary>
        /// <param name="timeFromNow">Time interval</param>
        public AbsoluteTime(TimeSpan timeFromNow)
            : this(DateTime.Now + timeFromNow)
        {
        }

        /// <summary>
        ///	Specifies if item has expired or not.
        /// </summary>
        /// <remarks>
        ///	bool isExpired = ICacheItemExpiration.HasExpired();
        /// </remarks>
        /// <returns>
        ///	"True", if the data item has expired or "false", if the data item 
        ///	has not expired
        /// </returns>
        public bool HasExpired()
        {
            // Convert to UTC in order to compensate for time zones		
            DateTime nowDateTime = DateTime.Now.ToUniversalTime();

            // Check expiration
            return nowDateTime.Ticks >= this.absoluteExpirationTime.Ticks;
        }

        /// <summary>
        /// Called to give this object an opportunity to initialize itself from data inside a CacheItem
        /// </summary>
        /// <param name="owningCacheItem">CacheItem provided to read initialization information from. Will never be null.</param>
        public void Initialize(CacheItem owningCacheItem)
        {
        }
    }

    /// <summary>
    ///	This provider tests if a item was expired using a time slice schema.
    /// </summary>
    [Serializable]    
    public class SlidingTime : ICacheItemExpiration
    {
        private DateTime timeLastUsed;
        private TimeSpan itemSlidingExpiration;

        /// <summary>
        ///	Create an instance of this class with the timespan for expiration.
        /// </summary>
        /// <param name="slidingExpiration">
        ///	Expiration time span
        /// </param>
        public SlidingTime(TimeSpan slidingExpiration)
        {
            // Check that expiration is a valid numeric value
            if (!(slidingExpiration.TotalSeconds >= 1))
            {
                throw new ArgumentOutOfRangeException("slidingExpiration");
            }

            this.itemSlidingExpiration = slidingExpiration;
        }


        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="slidingExpiration"/>
        /// <param name="originalTimeStamp"/>
		/// <remarks>
		/// This constructor is for testing purposes only. Never, ever call it in a real program
		/// </remarks>
        internal SlidingTime(TimeSpan slidingExpiration, DateTime originalTimeStamp) : this(slidingExpiration)
        {
            timeLastUsed = originalTimeStamp;
        }        

        /// <summary>
        /// Returns sliding time window that must be exceeded for expiration to occur
        /// </summary>
        public TimeSpan ItemSlidingExpiration
        {
            get { return itemSlidingExpiration; }
        }

        /// <summary>
        /// Returns time that this object was last touched
        /// </summary>
        public DateTime TimeLastUsed
        {
            get { return timeLastUsed; }
        }
		
		/// <summary>
        ///	Specifies if item has expired or not.
        /// </summary>
        /// <returns>Returns true if the item has expired otherwise false.</returns>
        public bool HasExpired()
        {
            bool expired = CheckSlidingExpiration(DateTime.Now,
                                                  this.timeLastUsed,
                                                  this.itemSlidingExpiration);
            return expired;
        }

        /// <summary>
        ///	Notifies that the item was recently used.
        /// </summary>
        public void Notify()
        {
            this.timeLastUsed = DateTime.Now;
        }

        /// <summary>
        /// Used to set the initial value of TimeLastUsed. This method is invoked during the reinstantiation of
        /// an instance from a persistent store. 
        /// </summary>
        /// <param name="owningCacheItem">CacheItem to which this expiration belongs.</param>
        public void Initialize(CacheItem owningCacheItem)
        {
        }

        /// <summary>
        ///	Check whether the sliding time has expired.
        /// </summary>
        /// <param name="nowDateTime">Current time </param>
        /// <param name="lastUsed">The last time when the item has been used</param>
        /// <param name="slidingExpiration">The span of sliding expiration</param>
        /// <returns>True if the item was expired, otherwise false</returns>
        private static bool CheckSlidingExpiration(DateTime nowDateTime,
                                                   DateTime lastUsed,
                                                   TimeSpan slidingExpiration)
        {
            // Convert to UTC in order to compensate for time zones
            DateTime tmpNowDateTime = nowDateTime.ToUniversalTime();

            // Convert to UTC in order to compensate for time zones
            DateTime tmpLastUsed = lastUsed.ToUniversalTime();

            long expirationTicks = tmpLastUsed.Ticks + slidingExpiration.Ticks;

            bool expired = (tmpNowDateTime.Ticks >= expirationTicks) ? true : false;

            return expired;
        }
    }

    /// <summary>
    ///	This class tracks a file cache dependency.
    /// </summary>
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(false)]
    public class FileDependency : ICacheItemExpiration
    {
        private readonly string dependencyFileName;

        private DateTime lastModifiedTime;

        /// <summary>
        ///	Constructor with one argument.
        /// </summary>
        /// <param name="fullFileName">
        ///	Indicates the name of the file
        /// </param>
        public FileDependency(string fullFileName)
        {
            if (string.IsNullOrEmpty(fullFileName))
            {
                throw new ArgumentException("fullFileName");
            }           

            dependencyFileName = Path.GetFullPath(fullFileName);
            EnsureTargetFileAccessible();

            if (!File.Exists(dependencyFileName))
            {
                throw new ArgumentException("fullFileName - Invalid file name!");
            }

            this.lastModifiedTime = File.GetLastWriteTime(fullFileName);
        }

		/// <summary>
		/// Gets the name of the dependent file.
		/// </summary>
		/// <value>
		/// The name of the dependent file.
		/// </value>
		public string FileName
		{
			get { return dependencyFileName; }
		}

		/// <summary>
		/// Gets the last modifed time of the file.
		/// </summary>
		/// <value>
		/// The last modifed time of the file
		/// </value>
		public DateTime LastModifiedTime
		{
			get { return lastModifiedTime; }
		}

        /// <summary>
        ///	Specifies if the item has expired or not.
        /// </summary>
        /// <returns>Returns true if the item has expired, otherwise false.</returns>
        public bool HasExpired()
        {
            EnsureTargetFileAccessible();

            if (File.Exists(this.dependencyFileName) == false)
            {
                return true;
            }

            DateTime currentModifiedTime = File.GetLastWriteTime(dependencyFileName);
            if (DateTime.Compare(lastModifiedTime, currentModifiedTime) != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        ///	Notifies that the item was recently used.
        /// </summary>
        public void Notify()
        {
        }

        /// <summary>
        /// Not used
        /// </summary>
        /// <param name="owningCacheItem">Not used</param>
        public void Initialize(CacheItem owningCacheItem)
        {
        }

        private void EnsureTargetFileAccessible()
        {
			// keep from changing during demand
			string file = dependencyFileName;
            FileIOPermission permission = new FileIOPermission(FileIOPermissionAccess.Read, file);
            permission.Demand();
        }
    }
}