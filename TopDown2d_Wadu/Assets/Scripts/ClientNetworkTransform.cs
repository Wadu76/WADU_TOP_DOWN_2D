using Unity.Netcode.Components; // 引入 Netcode 组件库
using UnityEngine;

//继承自NetworkTransform，通过改写它的权限规则来实现功能
[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    //重写这个方法：是否只允许服务器控制？
    //返回false，意味着：允许客户端自己控制（Client Authoritative）
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}