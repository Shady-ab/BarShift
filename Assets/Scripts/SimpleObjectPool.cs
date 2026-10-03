using System.Collections.Generic;
using UnityEngine;

public class SimpleObjectPool : MonoBehaviour
{
    private readonly Queue<GameObject> available = new Queue<GameObject>();
    private GameObject template;
    private Transform parent;

    public void Initialize(GameObject objectTemplate, int initialSize, Transform poolParent)
    {
        template = objectTemplate;
        parent = poolParent;

        for (int i = 0; i < initialSize; i++)
            available.Enqueue(CreateInstance());
    }

    public GameObject Get()
    {
        GameObject item = available.Count > 0 ? available.Dequeue() : CreateInstance();
        item.SetActive(true);
        return item;
    }

    public void Release(GameObject item)
    {
        if (item == null) return;
        item.SetActive(false);
        item.transform.SetParent(parent, false);
        available.Enqueue(item);
    }

    private GameObject CreateInstance()
    {
        GameObject item = Instantiate(template, parent);
        item.name = template.name.Replace("Prototype", "Pooled");
        item.SetActive(false);
        return item;
    }
}
