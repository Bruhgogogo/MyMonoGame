using System.Collections.Generic;

namespace MonoEngine.Class
{
    public sealed class GameObject : MonoObject
    {
        private static readonly Dictionary<string, GameObject> pathToGameObject = new Dictionary<string, GameObject>();
        private static int nextId = 0;

        private GameObject parent;
        private readonly List<Component> components = new List<Component>();
        private readonly List<GameObject> children = new List<GameObject>(32);
        private string name;
        private string path;

        public readonly int InstanceId;

        public GameObject Parent
        {
            get => parent;
            set
            {
                if (parent == value || value == this) return;
                if (value != null && value.IsChildOf(this)) return;

                if (parent != null)
                {
                    parent.children.Remove(this);
                    UnregisterSubtree();
                }

                parent = value;
                value?.children.Add(this);

                string parentPath = PathOf(parent);
                string uniqueName = MakeUniqueName(parentPath, name);
                if (uniqueName != name) name = uniqueName;
                RegisterSubtree(parentPath);
            }
        }

        public IReadOnlyList<GameObject> Childrens => children;

        public string Name
        {
            get => name;
            set
            {
                string newName = value ?? $"GameObject{InstanceId}";
                if (newName == name) return;

                UnregisterSubtree();
                string uniqueName = MakeUniqueName(PathOf(parent), newName);
                name = uniqueName;
                RegisterSubtree(PathOf(parent));
            }
        }

        public string Path => path;

        public GameObject() : this(null) { }

        public GameObject(string objectName)
        {
            InstanceId = nextId++;
            name = MakeUniqueName(null, objectName);
            RegisterSubtree(null);
        }

        public static GameObject Find(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return null;
            pathToGameObject.TryGetValue(fullPath, out var go);
            return go;
        }

        public GameObject FindChild(string childName)
        {
            if (this == null) return null;
            if (string.IsNullOrWhiteSpace(childName)) return this;

            string target = path == null ? childName : path + "/" + childName;
            pathToGameObject.TryGetValue(target, out var go);
            return go;
        }

        public bool IsChildOf(GameObject other)
        {
            if (other == null || parent == null) return false;
            for (GameObject p = parent; p != null; p = p.parent)
                if (p == other) return true;
            return false;
        }

        public bool GetComponentInChildren<T>(out T component) where T : Component
        {
            component = GetComponent<T>();
            if (component) return true;

            foreach (var child in children)
                if (child.GetComponentInChildren(out component))
                    return true;

            component = null;
            return false;
        }

        public T AddComponent<T>() where T : Component, new()
        {
            T component = new T();
            component.GO = this;
            components.Add(component);
            component.Awake();

            if (component is Transform t)
            {
                t.RecalculateCaches();
                t.PropagateToChildren();
            }
            return component;
        }

        public T GetOrAddComponent<T>() where T : Component, new()
        {
            T component = GetComponent<T>();
            if (!component) component = AddComponent<T>();
            return component;
        }

        public T GetComponent<T>() where T : Component
        {
            foreach (var comp in components)
                if (comp is T t && comp)
                    return t;
            return null;
        }

        public List<T> GetComponents<T>() where T : Component
        {
            var result = new List<T>();
            foreach (var comp in components)
                if (comp is T t && comp)
                    result.Add(t);
            return result;
        }

        private static string PathOf(GameObject go) => go?.path;

        private static string MakeUniqueName(string parentPath, string desired)
        {
            desired = string.IsNullOrWhiteSpace(desired) ? $"GameObject{nextId}" : desired;
            if (!HasNameInParent(parentPath, desired)) return desired;

            string candidate = desired + InstanceIdOfScope(nextId);
            if (!HasNameInParent(parentPath, candidate)) return candidate;

            int n = 1;
            while (HasNameInParent(parentPath, desired + InstanceIdOfScope(nextId) + n)) n++;
            return desired + InstanceIdOfScope(nextId) + n;
        }

        private static int InstanceIdOfScope(int seed) => seed;

        private static bool HasNameInParent(string parentPath, string childName)
        {
            string key = parentPath == null ? childName : parentPath + "/" + childName;
            return pathToGameObject.ContainsKey(key);
        }

        private void RegisterSubtree(string parentPath)
        {
            path = parentPath == null ? name : parentPath + "/" + name;
            pathToGameObject[path] = this;
            foreach (var child in children)
                child.RegisterSubtree(path);
        }

        private void UnregisterSubtree()
        {
            if (path == null) return;
            pathToGameObject.Remove(path);
            foreach (var child in children)
                child.UnregisterSubtree();
        }

        protected override void OnDestroy()
        {
            UnregisterSubtree();

            for (int i = children.Count - 1; i >= 0; i--)
            {
                GameObject child = children[i];
                child.parent = null;
                Transform ct = child.GetComponent<Transform>();
                if (ct)
                {
                    ct.RecalculateCaches();
                    ct.PropagateToChildren();
                }
            }
            children.Clear();

            if (parent)
            {
                parent.children.Remove(this);
                parent = null;
            }

            foreach (var comp in components)
            {
                if (comp)
                {
                    if (comp.Enabled) comp.OnDisable();
                    comp.Destroy();
                }
            }
            components.Clear();
        }
    }
}