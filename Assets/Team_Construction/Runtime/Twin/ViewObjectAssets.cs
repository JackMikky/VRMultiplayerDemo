using System;
using System.Collections.Generic;
using UnityEngine;

namespace PXR.Construction.Runtime
{
    [CreateAssetMenu(fileName = "ViewObjectAssets", menuName = "Scriptable Objects/ViewObjectAssets")]
    public class ViewObjectAssets : ScriptableObject
    {
        [SerializeField] private List<Asset> assets = new();

        [Serializable]
        public class Asset
        {
            public GameObject GameObject;
        }

        public Asset this[int idx]
        {
            get { return assets[idx]; }
        }

        public int Count => assets.Count;
    }
}