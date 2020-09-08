using System;
using Akka.Actor;
using FLServer.Actors.Player.State.Base;


namespace FLServer.Actors.Player.State
{
    partial class State
    {

        ActorRef _baseRef;

        public void InBaseState(object message)
        {
            //TODO: handle discon in base state, do save and gracefully exit
            //for InSpaceState we'll need to remove SimObj and/or
            //add some kind of timer so player won't F1 into nowhere
            //keep in mind, client reports awfully big position coords before discon
            var msg = (byte[])message;
            if (msg[0] == 0x01 && msg.Length == 1)
            {
                // Keepalive
                //byte[] omsg = { 0xFF };
                Context.Sender.Tell(new byte[] { 0xFF });
            }
            else if (msg.Length >= 2)
            {
                int type = msg[0] << 8 | msg[1];
                switch (type)
                {
                    case 0x0101:
						//RxCommonUpdateObject(player, msg);
                        break;
                    case 0x0201:
						//RxCommonFireWeapon(player, msg);
                        break;
                    case 0x0401:
						//RxSetTarget(player, msg);
                        break;
                    case 0x0501:
						//RxChat(player, msg);
                        break;
                    case 0x0801:
						//RxActivateEquip(player, msg);
                        break;
                    case 0x0E01:
						//RxActivateCruise(player, msg);
                        break;
                    case 0x0F01:
						//RxGoTradelane(player, msg);
                        break;
                    case 0x1001:
						//RxStopTradelane(player, msg);
                        break;
                    case 0x1101:
						//RxSetWeaponsGroup(player, msg);
                        break;
                    case 0x1301:
						//RxSetVisitedState(player, msg);
                        break;
                    case 0x1401:
						//RxJettisionCargo(player, msg);
                        break;
                    case 0x1501:
						//RxActivateThrusters(player, msg);
                        break;
                    case 0x1601:
						//RxRequestBestPath(player, msg);
                        break;
                    case 0x1701:
						//RxRequestNavMap(player, msg);
                        break;
                    case 0x1801:
                        Context.Parent.Tell(new PlayerStatsRequest());
						//RxRequestPlayerStats(player, msg);
                        break;
                    case 0x1A01:
						//RxRequestRank
						//FLPACKET_COMMON_REQUEST_RANK_LEVEL
						//answer: 1A 01 04 00 00 00 FF FF FF FF
						_baseRef.Tell(new RankRequest(), Context.Sender);
                        break;
                    case 0x1c01:
						//RxSetInterfaceState(player, msg);
                        break;

                    case 0x0303:
						//RxMunitionCollision(player, msg);
                        break;
                    case 0x0403:
						//RxRequestLaunch(player, msg);
                        break;
                    case 0x0503:
						//RxRequestCharInfo(player, msg);
                        break;
                    case 0x0703:
						//RxEnterBase(player, msg);
                        {
                            var pos = 2;
                            var baseid = FLMsgType.GetUInt32(msg, ref pos);

                            var acc = Context.Parent.Ask<PlayerActor.AccountShipData>(new PlayerActor.EnterBase(baseid),
                                          TimeSpan.FromMilliseconds(850));
                            _baseRef.Tell(new EnterBaseData(acc.Result.Account, acc.Result.ShipData, baseid), Context.Sender);
                        }
						//_baseID = baseid;
						
                        break;
                    case 0x0803:
						//RxRequestBaseInfo(msg);
                        _baseRef.Tell(new BaseInfoRequest(msg), Context.Sender);
                        break;
                    case 0x0903:
						//RxRequestLocationInfo(player, msg);
                        _baseRef.Tell(new LocInfoRequest(msg), Context.Sender);
                        break;
                    case 0x0B03:
						//RxSystemSwitchOutComplete(player, msg);
                        break;
                    case 0x0C03:
						//RxObjectCollision(player, msg);
                        break;
                    case 0x0D03:
						//RxExitBase(player, msg);
                        break;
                    case 0x0E03:
						//RxEnterLocation(msg);
                        {
                            int pos = 2;
                            uint roomid = FLMsgType.GetUInt32(msg, ref pos);
                            _baseRef.Tell(new EnterLocation(roomid), Context.Sender);
                        }
                        break;
                    case 0x0F03:
						//RxExitLocation(player, msg);
                        {
                            int pos = 2;
                            uint roomid = FLMsgType.GetUInt32(msg, ref pos);
                            _baseRef.Tell(new ExitLocation(roomid), Context.Sender);
                        }
                        break;
                    case 0x1003:
						//RxRequestCreateShip(player, msg);
                        break;
                    case 0x1103:
						//RxGoodSell(player, msg); - we ignore this, look RxRequestRemoveItem
                        break;
                    case 0x1203:
						//RxGoodBuy(player, msg); - we ignore this, look RxRequestAddItem
                        break;
                    case 0x1303:
						//RxGFSelectObject(player, msg);
                        {
                            var pos = 2;
                            var charid = FLMsgType.GetUInt32(msg, ref pos);
                            _baseRef.Tell(new SelectObject(charid));
                        }
                        break;

                    case 0x1403:
						//RxMissionResponse(player, msg);
                        break;
                    case 0x1503:
						//RxRequestShipArch(player, msg);
                        break;
                    case 0x1603:
						//RxRequestEquipment(player, msg);
                        break;
                    case 0x1803:
						//RxRequestAddItem(player, msg);
                        _baseRef.Tell(new RequestAddItem(msg), Context.Sender);
                        break;
                    case 0x1903:
						//RxRequestRemoveItem(player, msg);
                        break;
                    case 0x1B03:
						//RxRequestSetCash(player, msg); - we ignore this, count based on buy\sell, RxRequest{add,remove}Item
                        break;
                    case 0x1C03:
						//RxRequestChangeCash(player, msg); - we ignore this, count based on buy\sell, RxRequest{add,remove}Item
                        break;
                    case 0x2F03:
						//RxSetManeuver(player, msg);
                        break;
                    case 0x3103:
						//RxRequestEvent(player, msg);
                        break;
                    case 0x3203:
						//RxRequestCancel(player, msg);
                        break;
                    case 0x3b03:
						//RxRequestSetHullStatus(player, msg);
                        break;
                    case 0x3E03:
						//RxLaunchComplete(player, msg);
                        break;
                    case 0x3F03:
						//RxClientHail(player, msg);
                        break;
                    case 0x4003:
						//RxRequestUseItem(player, msg);
                        break;
                    case 0x4303:
						//RxJumpInComplete(player, msg);
                        break;
                    case 0x4403:
						//RxRequestInvincibility(player, msg);
                        break;
                    default:
						// Unexpected packet. Log and ignore it.
                        _log.Warn("Unexpected message from FLID {0} in InBaseState: {1}", _flPlayerID, BitConverter.ToString(msg));
                        break;
                }
            }
            else
            {
                // Unexpected packet. Log and ignore it.
                _log.Warn("Unexpected message from FLID {0} in InBaseState: {1}", _flPlayerID, BitConverter.ToString(msg));
            }
        }





    }
}
