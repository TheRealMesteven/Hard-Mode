﻿using HarmonyLib;
using PulsarModLoader;

namespace Hard_Mode
{
    class Options //For all future options
    {
        public static bool FogOfWar = false;
        public static bool DangerousReactor = false;
        public static bool MasterHasMod = false;
        public static bool WeakReactor = false;
    }

    class ReciveOptions : ModMessage //Recive the options from the master client
    {
        public override void HandleRPC(object[] arguments, PhotonMessageInfo sender)
        {
            Options.FogOfWar = (bool)arguments[0];
            Options.DangerousReactor = (bool)arguments[1];
            Options.MasterHasMod = (bool)arguments[2];
            Options.WeakReactor = (bool)arguments[3];
        }
    }
    [HarmonyPatch(typeof(PLServer), "ServerSendClientStarmap")]
    class UpdateMap //Updates the galaxy to the clients
    {
        static void Postfix(PhotonMessageInfo pmi) 
        {
            foreach (PLSectorInfo sector in PLGlobal.Instance.Galaxy.AllSectorInfos.Values) 
            {
                if (sector.Discovered)
                {
                    PLServer.Instance.photonView.RPC("ClientSectorInfo", pmi.sender, new object[]
                    {
                      sector.ID,
                      sector.Discovered,
                      sector.Visited
                    });
                }
            }
        }
    }

    [HarmonyPatch(typeof(PLGlobal), "EnterNewGame")]
    class OnJoin //This should reset the configs everytime you enter a game and is not the host, so you don't end up with the effects in not modded games
    {
        static void Postfix()
        {
            if (!PhotonNetwork.isMasterClient) 
            {
                Options.MasterHasMod = false;
                Options.FogOfWar = false;
                Options.DangerousReactor = false;
                Options.WeakReactor = false;
            }
            else 
            {
                Options.MasterHasMod = true;
                string savedoptions = PLXMLOptionsIO.Instance.CurrentOptions.GetStringValue("HardModeOptions");
                if (savedoptions != string.Empty)
                {
                    string[] array = savedoptions.Split(new char[]
                    {
                    ','
                    });
                    for(int i = 0; i < array.Length; i++) 
                    {
                        switch (i) 
                        {
                            case 0:
                                Options.FogOfWar = bool.Parse(array[0]);
                                break;
                            case 1:
                                Options.DangerousReactor = bool.Parse(array[1]);
                                break;
                            case 2:
                                Options.WeakReactor = bool.Parse(array[2]);
                                break;
                        }
                    }
                }
            }
        }
    }
}
