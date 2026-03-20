using System.Threading.Tasks;
using TMPro; // 引入 UI 命名空间
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RelayManager : MonoBehaviour
{
    [Header("UI 绑定")]
    public TMP_InputField joinCodeInput; // 客户端用来输入的框
    //用于保存房间号
    public static string RoomCode = "";

    //用来在屏幕上显示房间码的文本
    public TextMeshProUGUI roomCodeDisplayText;

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("已匿名登录 Unity Cloud");
        }
    }

    public async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            //把获取到的码存进静态变量里
            RoomCode = joinCode;

            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("建房失败: " + e.Message);
        }
    }

    public void JoinGameWithUI()
    {
        if (joinCodeInput != null && !string.IsNullOrEmpty(joinCodeInput.text))
        {
            JoinRelay(joinCodeInput.text);
        }
        else
        {
            Debug.LogWarning("房间码不能为空！");
        }
    }

    private async void JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("加入失败，请检查房间码: " + e.Message);
        }
    }
}