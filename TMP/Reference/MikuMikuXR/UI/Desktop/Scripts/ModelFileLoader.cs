using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;
using LibMMD.Unity3D;
using System.IO;

public class ModelFileLoader : MonoBehaviour
{
    void Start()
    {
        // 自动查找名为BtnAddModel的按钮
        var btn = GameObject.Find("BtnAddModel");
        if (btn != null)
        {
            btn.GetComponent<Button>().onClick.AddListener(OnAddModelClicked);
        }
        else
        {
            Debug.LogError("未找到BtnAddModel按钮");
        }
    }

    public void OnAddModelClicked()
    {
        // 设置文件过滤器，只显示PMD/PMX
        FileBrowser.SetFilters(true, new FileBrowser.Filter("MMD模型", ".pmd", ".pmx"));
        FileBrowser.SetDefaultFilter(".pmx");
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");

        // 弹出文件选择器
        FileBrowser.ShowLoadDialog(
            (paths) => { OnModelFileSelected(paths); },
            () => { Debug.Log("取消选择模型文件"); },
            FileBrowser.PickMode.Files,
            false, // 不允许多选
            null, null, "选择MMD模型", "加载"
        );
    }

    void OnModelFileSelected(string[] paths)
    {
        if (paths == null || paths.Length == 0)
            return;

        string modelPath = paths[0];
        // 创建MMD模型GameObject并加载，确保有SkinnedMeshRenderer和MeshFilter
        var go = new GameObject("MMDModel");
        go.AddComponent<SkinnedMeshRenderer>();
        go.AddComponent<MeshFilter>();
        var mmdGo = go.AddComponent<MmdGameObject>();
        bool success = mmdGo.LoadModel(modelPath);
        if (!success)
        {
            Debug.LogError("模型加载失败: " + modelPath);
            Destroy(go);
        }
        else
        {
            // 挂到Actors下
            var actors = GameObject.Find("Actors");
            if (actors != null)
            {
                go.transform.SetParent(actors.transform, false);
            }
            else
            {
                Debug.LogWarning("未找到Actors对象，模型将放在场景根节点");
            }
            Debug.Log("模型加载成功: " + modelPath);
            // 刷新下拉列表
            var fileManager = FindObjectOfType<MikuMikuXR.UI.Desktop.UGUIFileManager>();
            if (fileManager != null)
            {
                fileManager.UpdateModelList();
            }
        }
    }
}
