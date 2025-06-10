using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using MMDVR.Scripts.UIInteraction;
using MMDVR.Managers; // Now correctly using the MusicManager namespace

// MusicData now primarily serves as a bridge to IResourceInfo for UI purposes,
// using data obtained from MusicManager.MusicTrackInfo
public class MusicData : IResourceInfo
{
    public string id; // Corresponds to MusicTrackInfo.ID
    public string title; // Corresponds to MusicTrackInfo.DisplayName
    public string filePath; // Corresponds to MusicTrackInfo.FilePath

    public string ID => id;
    public string DisplayName => title;
    public string FilePath => filePath;
    public ResourceType Type => ResourceType.Music;
}

public class MusicListController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject listItemPrefab;
    public Transform listContainer;
    public DropZone listSortableAreaDropZone;
    public DropZone uninstallDropZone;

    [Header("Manager References")]
    public MusicManager musicManager; // Assign MusicManager.Instance in Start or Inspector

    private List<GameObject> uiListItemObjects = new List<GameObject>();
    private List<IResourceInfo> internalResourceList = new List<IResourceInfo>();

    void Start()
    {
        if (musicManager == null) musicManager = MusicManager.Instance;

        if (listItemPrefab == null || listContainer == null || musicManager == null)
        {
            Debug.LogError("MusicListController: UI References or MusicManager not set!");
            enabled = false; // Disable component if setup is invalid
            return;
        }

        if (listSortableAreaDropZone != null)
        {
            listSortableAreaDropZone.onItemDropped.AddListener(HandleDropOnListArea);
        }
        if (uninstallDropZone != null)
        {
            uninstallDropZone.onItemDropped.AddListener(HandleDropOnUninstallZone);
        }

        LoadAndDisplayItems();
    }

    void LoadAndDisplayItems()
    {
        // 1. Clear existing UI items and internal list
        foreach (Transform child in listContainer)
        {
            Destroy(child.gameObject);
        }
        uiListItemObjects.Clear();
        internalResourceList.Clear();

        // 2. Fetch data from MusicManager
        if (musicManager == null) return;

        List<MusicTrackInfo> loadedTracks = musicManager.GetLoadedMusicTrackInfos();
        if (loadedTracks == null || loadedTracks.Count == 0)
        {
            // Optionally, load some default/test music if the list is empty
            // For now, we just log and show an empty list.
            Debug.Log("MusicListController: No music loaded in MusicManager. Displaying empty list.");
            // Example: Load a test music file if you have one
            // string testMusicPath = "C:/Path/To/Your/Music/File_Test.mp3"; // <<< USER NEEDS TO CHANGE THIS
            // if (System.IO.File.Exists(testMusicPath))
            // {
            //     string newId = musicManager.LoadMusic(testMusicPath);
            //     if (!string.IsNullOrEmpty(newId))
            //     {
            //         loadedTracks = musicManager.GetLoadedMusicTrackInfos(); // Refresh after loading
            //     }
            // }
        }

        foreach (var trackInfo in loadedTracks)
        {
            internalResourceList.Add(new MusicData
            {
                id = trackInfo.ID,
                title = trackInfo.DisplayName,
                filePath = trackInfo.FilePath
            });
        }

        RefreshResourceListUI();
    }

    void RefreshResourceListUI()
    {
        foreach (Transform child in listContainer)
        {
            Destroy(child.gameObject);
        }
        uiListItemObjects.Clear();

        for (int i = 0; i < internalResourceList.Count; i++)
        {
            IResourceInfo resourceData = internalResourceList[i];
            GameObject listItemGO = Instantiate(listItemPrefab, listContainer);
            listItemGO.name = resourceData.Type + "_Item_" + resourceData.DisplayName.Replace(" ", "");

            DraggableItem draggableItem = listItemGO.GetComponent<DraggableItem>();
            if (draggableItem != null)
            {
                draggableItem.Data = resourceData;
            }

            TextMeshProUGUI titleText = listItemGO.GetComponentInChildren<TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.text = resourceData.DisplayName;
            }

            uiListItemObjects.Add(listItemGO);
            UpdateItemVisual(listItemGO, resourceData, i == 0 && resourceData.ID == musicManager.GetCurrentTrackInfo()?.ID);
        }
    }

    public void HandleDropOnListArea(GameObject droppedGameObject)
    {
        DraggableItem droppedItem = droppedGameObject.GetComponent<DraggableItem>();
        if (droppedItem == null || droppedItem.Data == null) return;

        IResourceInfo droppedResourceData = droppedItem.Data;

        int newIndex = 0;
        for (int i = 0; i < listContainer.childCount; i++)
        {
            if (listContainer.GetChild(i) == droppedGameObject.transform)
            {
                newIndex = i;
                break;
            }
        }

        droppedGameObject.transform.SetParent(listContainer);
        droppedGameObject.transform.SetSiblingIndex(newIndex);

        internalResourceList.Remove(droppedResourceData);
        internalResourceList.Insert(newIndex, droppedResourceData);

        Debug.Log($"{droppedResourceData.DisplayName} moved to index {newIndex} in {listContainer.name}");

        if (newIndex == 0 && droppedResourceData.Type == ResourceType.Music)
        {
            ActivateResource(droppedResourceData);
        }
        else if (droppedResourceData.Type == ResourceType.Music && musicManager.GetCurrentTrackInfo()?.ID == droppedResourceData.ID)
        {
            // If the currently playing song was moved but is no longer first, stop it or handle as per desired logic.
            // For now, if it's not first, it won't be auto-played by the newIndex == 0 logic.
            // If it *was* playing and is moved from index 0, it will continue playing unless explicitly stopped.
            // The MusicManager.PlayMusicById will handle stopping others.
        }

        UpdateAllItemVisuals();
    }

    public void HandleDropOnUninstallZone(GameObject droppedGameObject)
    {
        DraggableItem droppedItem = droppedGameObject.GetComponent<DraggableItem>();
        if (droppedItem != null && droppedItem.Data != null)
        {
            IResourceInfo resourceToUninstall = droppedItem.Data;
            Debug.Log($"Requesting uninstall for: {resourceToUninstall.DisplayName} (Type: {resourceToUninstall.Type})");

            if (resourceToUninstall.Type == ResourceType.Music)
            {
                musicManager?.UninstallMusic(resourceToUninstall.ID);
                Debug.Log($"Called MusicManager.UninstallMusic for: {resourceToUninstall.DisplayName}");
            }
            // Add else if for other resource types here

            internalResourceList.Remove(resourceToUninstall);
            uiListItemObjects.Remove(droppedGameObject);
            Destroy(droppedGameObject);

            // If the uninstalled item was the active one, and there are other items,
            // activate the new first item if it's music.
            if (internalResourceList.Count > 0)
            {
                if (internalResourceList[0].Type == ResourceType.Music)
                {
                    // Check if the uninstalled item was the one playing.
                    // If so, the MusicManager.UninstallMusic should have handled stopping it.
                    // Now, activate the new first item.
                    ActivateResource(internalResourceList[0]);
                }
            }
            else if (resourceToUninstall.Type == ResourceType.Music)
            {
                // List is empty, ensure music is stopped
                musicManager?.StopAllMusic();
                Debug.Log("Music list empty, stopping all playback via MusicManager.");
            }
            UpdateAllItemVisuals();
        }
    }

    void ActivateResource(IResourceInfo resourceData)
    {
        if (musicManager == null) return;

        if (resourceData.Type == ResourceType.Music)
        {
            Debug.Log($"Activating Music: {resourceData.DisplayName} with ID: {resourceData.ID}");
            musicManager.PlayMusicById(resourceData.ID);
        }
        // Add else if for other resource types

        UpdateAllItemVisuals();
    }

    void UpdateAllItemVisuals()
    {
        if (musicManager == null) return;
        string currentPlayingMusicID = musicManager.GetCurrentTrackInfo()?.ID;

        for (int i = 0; i < listContainer.childCount; i++)
        {
            GameObject uiItemGO = listContainer.GetChild(i).gameObject;
            DraggableItem draggable = uiItemGO.GetComponent<DraggableItem>();
            if (draggable != null && draggable.Data != null)
            {
                IResourceInfo resourceData = draggable.Data;
                bool isActive = false;
                if (resourceData.Type == ResourceType.Music)
                {
                    // A music item is "active" if it is currently playing according to MusicManager
                    isActive = (resourceData.ID == currentPlayingMusicID);
                    // Additionally, the first item in the list has a distinct visual style if it's the one set to play.
                    // The actual playback is handled by ActivateResource -> musicManager.PlayMusicById.
                    // The visual distinction for being "first and potentially active" is handled here.
                    if (i == 0 && internalResourceList.Count > 0 && internalResourceList[0].ID == resourceData.ID)
                    {
                        // This item is at the top of the list. Its active state (color) depends on whether it's actually playing.
                    }
                }
                // For other resource types, define their active state logic
                UpdateItemVisual(uiItemGO, resourceData, isActive);
            }
        }
    }

    void UpdateItemVisual(GameObject itemGO, IResourceInfo resourceData, bool isActive)
    {
        UnityEngine.UI.Image bgImage = itemGO.GetComponent<UnityEngine.UI.Image>();
        if (bgImage != null)
        {
            // Active (playing) items are yellow. First item in list (if not playing) could be light-yellow or default.
            // For simplicity: yellow if active (playing), white otherwise.
            // You might want a different color for "next up" (first in list but not playing).
            bgImage.color = isActive ? Color.yellow : Color.white;
        }

        Transform activeIndicator = itemGO.transform.Find("ActiveIndicator");
        if (activeIndicator != null)
        {
            activeIndicator.gameObject.SetActive(isActive);
        }
        // Debug.Log($"{itemGO.name} ({resourceData.DisplayName}) isActive: {isActive}");
    }
    
    // Public method to be called by a UI Button or other event to manually load a new music file.
    public void TriggerLoadNewMusicDialog()
    { 
        // This is a placeholder for file dialog logic.
        // You would use a native file dialog plugin or Unity's EditorUtility (editor only)
        // For a runtime solution, you might need a custom input field for path or a plugin.
        Debug.Log("TriggerLoadNewMusicDialog called. Implement file selection logic here.");
        // Example: string filePath = ShowFileDialog(); // (This function doesn't exist, needs implementation)
        // if (!string.IsNullOrEmpty(filePath) && musicManager != null)
        // {
        //     string newId = musicManager.LoadMusic(filePath);
        //     if (!string.IsNullOrEmpty(newId))
        //     {
        //         // Find the new track info
        //         MusicTrackInfo newTrack = musicManager.GetMusicTrackInfoById(newId);
        //         if (newTrack != null)
        //         {
        //             // Add to UI list and refresh
        //             internalResourceList.Add(new MusicData 
        //             { 
        //                 id = newTrack.ID, 
        //                 title = newTrack.DisplayName, 
        //                 filePath = newTrack.FilePath 
        //             });
        //             RefreshResourceListUI();
        //             // Optionally, make it active if it's the only one or based on user preference
        //             if (internalResourceList.Count == 1) {
        //                 ActivateResource(internalResourceList[0]);
        //             }
        //         }
        //     }
        // }
    }
}
