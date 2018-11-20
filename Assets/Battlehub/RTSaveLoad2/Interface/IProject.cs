﻿using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;
namespace Battlehub.RTSaveLoad2.Interface
{
    public delegate void ProjectEventHandler(Error error);
    public delegate void ProjectEventHandler<T>(Error error, T result);
    public delegate void ProjectEventHandler<T, T2>(Error error, T result, T2 result2);

    public interface IProject
    {
        event ProjectEventHandler OpenCompleted;
        event ProjectEventHandler<ProjectItem[]> GetAssetItemsCompleted;
        event ProjectEventHandler<AssetItem> CreatePrefabCompleted;
        event ProjectEventHandler<AssetItem[]> SaveCompleted;
        event ProjectEventHandler<UnityObject> LoadCompleted;
        event ProjectEventHandler UnloadCompleted;
        event ProjectEventHandler<AssetItem[]> ImportCompleted;
        event ProjectEventHandler<ProjectItem[]> DeleteCompleted;
        event ProjectEventHandler<ProjectItem[], ProjectItem> MoveCompleted;
        event ProjectEventHandler<ProjectItem> RenameCompleted;

        ProjectItem Root
        {
            get;
        }

        string[] AssetLibraries
        {
            get;
        }

        bool IsStatic(ProjectItem projectItem);
        Type ToType(AssetItem assetItem);
        Guid ToGuid(Type type);
        long ToID(UnityObject obj);
        T FromID<T>(long id) where T : UnityObject;

        string GetExt(object obj);
        string GetExt(Type type);

        ProjectAsyncOperation Open(string project, ProjectEventHandler callback = null);
        ProjectAsyncOperation<ProjectItem[]> GetAssetItems(ProjectItem[] folders, ProjectEventHandler<ProjectItem[]> callback = null);

        ProjectAsyncOperation<AssetItem> CreatePrefab(ProjectItem parent, byte[] previewData, object obj, string nameOverride, ProjectEventHandler<AssetItem> callback = null);
        ProjectAsyncOperation<AssetItem[]> Save(AssetItem[] assetItems, object[] objects, ProjectEventHandler<AssetItem[]> callback = null);
        ProjectAsyncOperation<UnityObject> Load(AssetItem assetItem, ProjectEventHandler<UnityObject> callback = null);
        AsyncOperation Unload(ProjectEventHandler completedCallback = null);

        ProjectAsyncOperation<ProjectItem> LoadAssetLibrary(int index, ProjectEventHandler<ProjectItem> callback = null);
        ProjectAsyncOperation<AssetItem[]> Import(ImportItem[] assetItems, ProjectEventHandler<AssetItem[]> callback = null);
        ProjectAsyncOperation<ProjectItem> Rename(ProjectItem projectItem, string oldName, ProjectEventHandler<ProjectItem> callback = null);
        ProjectAsyncOperation<ProjectItem[], ProjectItem> Move(ProjectItem[] projectItems, ProjectItem target, ProjectEventHandler<ProjectItem[], ProjectItem> callback = null);
        ProjectAsyncOperation<ProjectItem[]> Delete(ProjectItem[] projectItems, ProjectEventHandler<ProjectItem[]> callback = null);
    }

    public class ProjectAsyncOperation : CustomYieldInstruction
    {
        public Error Error
        {
            get;
            set;
        }
        public bool IsCompleted
        {
            get;
            set;
        }
        public override bool keepWaiting
        {
            get { return !IsCompleted; }
        }
    }

    public class ProjectAsyncOperation<T> : ProjectAsyncOperation
    {
        public T Result
        {
            get;
            set;
        }
    }

    public class ProjectAsyncOperation<T, T2> : ProjectAsyncOperation<T>
    {
        public T2 Result2
        {
            get;
            set;
        }
    }
}