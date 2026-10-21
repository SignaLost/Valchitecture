using System.Collections;
using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;
using Valchitecture.Common.Configuration;

namespace Valchitecture.Modules.Modes;

/// <summary>Per piece support switches; the server's list applies, sent on join and on change.</summary>
internal static class SupportList
{
    private static readonly HashSet<int> _fromServer = new();

    private static CustomRPC _rpc;

    /// <summary>The connection <see cref="_fromServer" /> was received on; stale once it closes.</summary>
    private static ZNet _receivedOn;

    internal static void Init()
    {
        _rpc = NetworkManager.Instance.AddRPC("SupportList", OnServerReceive, OnClientReceive);
        SynchronizationManager.Instance.AddInitialSynchronization(_rpc, Package);
    }

    /// <summary>Whether this prefab is set to support: by the server's list on a client, else the local config.</summary>
    internal static bool Allows(int prefab)
    {
        ZNet net = ZNet.instance;

        if (net && !net.IsServer())
        {
            return _receivedOn == net && _fromServer.Contains(prefab);
        }

        return PieceConfig.Supports(prefab);
    }

    /// <summary>Sends the list to all peers. Server only.</summary>
    internal static void Broadcast()
    {
        ZNet net = ZNet.instance;

        if (_rpc != null && net && net.IsServer() && net.m_peers.Count > 0)
        {
            _rpc.SendPackage(net.m_peers, Package());
        }
    }

    private static ZPackage Package()
    {
        List<int> chosen = PieceConfig.SupportingPieces();
        ZPackage package = new();
        package.Write(chosen.Count);

        foreach (int prefab in chosen)
        {
            package.Write(prefab);
        }

        return package;
    }

    private static IEnumerator OnClientReceive(long sender, ZPackage package)
    {
        // Routed RPCs can come from any peer.
        if (ZRoutedRpc.instance == null || sender != ZRoutedRpc.instance.GetServerPeerID())
        {
            yield break;
        }

        _fromServer.Clear();

        for (int count = package.ReadInt(); count > 0; count--)
        {
            _fromServer.Add(package.ReadInt());
        }

        _receivedOn = ZNet.instance;
        yield break;
    }

    // Clients never send the list.
    private static IEnumerator OnServerReceive(long sender, ZPackage package)
    {
        yield break;
    }
}
