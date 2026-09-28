using System.Collections.Generic;
using UnityEngine;

namespace ACR.Core
{
    /// <summary>
    /// Minimal generic object pool. Required from the first weapon implementation onward — spawning
    /// and destroying projectiles/particles at runtime is one of the most common mobile performance
    /// killers in this genre, so pooling isn't something we retrofit once it becomes a problem.
    ///
    /// Serves Pillars: "Readable Chaos" & Mobile Optimization.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly Queue<T> available = new Queue<T>();
        private readonly T prefab;
        private readonly Transform parent;

        public int AvailableCount => available.Count;

        public ObjectPool(T prefab, int prewarmCount, Transform parent = null)
        {
            this.prefab = prefab;
            this.parent = parent;

            for (int i = 0; i < prewarmCount; i++)
            {
                T instance = Object.Instantiate(prefab, parent);
                instance.gameObject.SetActive(false);
                available.Enqueue(instance);
            }
        }

        public T Get()
        {
            T instance = available.Count > 0 ? available.Dequeue() : Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Release(T instance)
        {
            if (instance == null) return;
            instance.gameObject.SetActive(false);
            available.Enqueue(instance);
        }
    }
}
