﻿using System;
using System.Collections.Generic;
using System.Timers;
using GTANetworkServer;
using GTANetworkShared;
using RoleplayServer.resources.core;
using RoleplayServer.resources.player_manager;
using RoleplayServer.resources.vehicle_manager;
using Vehicle = RoleplayServer.resources.vehicle_manager.Vehicle;

namespace RoleplayServer.resources.job_manager.taxi
{
    public class TaxiJob : Script
    {
        public const int MinFare = 5;
        public const int MaxFare = 50;

        public static List<Character> OnDutyDrivers = new List<Character>();
        public static List<Character> TaxiRequests = new List<Character>();

        public TaxiJob()
        {
            API.onPlayerEnterVehicle += API_onPlayerEnterVehicle;
            API.onPlayerExitVehicle += API_onPlayerExitVehicle;
            API.onClientEventTrigger += API_onClientEventTrigger;
        }

        private void API_onClientEventTrigger(Client player, string eventName, params object[] arguments)
        {
            switch (eventName)
            {
                case "update_taxi_destination":
                    Character character = API.getEntityData(player.handle, "Character");
                    API.triggerClientEvent(character.TaxiDriver.Client, "set_taxi_waypoint", (Vector3)arguments[0]);

                    API.sendChatMessageToPlayer(player, Color.Yellow, "[TAXI] You have successfully set your destination.");
                    API.sendChatMessageToPlayer(character.TaxiDriver.Client, "[TAXI] " + character.rp_name() + " has set the destination.");
                    break;
            }
        }

        private void API_onPlayerExitVehicle(Client player, NetHandle vehicle)
        {
            Character character = API.getEntityData(player.handle, "Character");
            var veh = VehicleManager.GetVehFromNetHandle(vehicle);

            if(veh != null)
            {
                if (OnDutyDrivers.Contains(character) && veh.Job.Type == JobManager.TaxiJob)
                {
                    API.sendChatMessageToPlayer(player, Color.Yellow, "[TAXI] You have left your taxi. Please return to it within 60 seconds or you will be taken off-duty and it will respawn.");

                    veh.RespawnTimer = new Timer {Interval = 1000 * 60};
                    veh.RespawnTimer.Elapsed += delegate { RespawnTaxi(character, veh); };
                    veh.RespawnTimer.Start();
                }

                if(veh.Driver != null && character.TaxiDriver != null)
                {
                    if (veh.Driver == character.TaxiDriver)
                    {
                        veh.Driver.Money += character.TotalFare;
                        character.Money -= character.TotalFare;

                        veh.Driver.Save();
                        character.Save();

                        API.sendChatMessageToPlayer(player, "~y~[TAXI] You have been charged $" + character.TotalFare + " for your taxi ride.");
                        API.sendChatMessageToPlayer(veh.Driver.Client, "~y~[TAXI] You have been paid $" + character.TotalFare + " for your services.");

                        API.triggerClientEvent(player, "update_fare_display", 0, 0, "");
                        API.triggerClientEvent(veh.Driver.Client, "update_fare_display", 0, 0, "");

                        veh.Driver.TaxiPassenger = null;
                        character.TaxiDriver = null;
                        character.TaxiTimer.Stop();
                        character.TotalFare = 0;
                    }
                }
            }
        }

        private void API_onPlayerEnterVehicle(Client player, NetHandle vehicle)
        {
            Character character = API.getEntityData(player.handle, "Character");
            var veh = VehicleManager.GetVehFromNetHandle(vehicle);

            //Cancel taxi car respawn 
            if (OnDutyDrivers.Contains(character) && veh.Job.Type == JobManager.TaxiJob)
            {
                if (veh.RespawnTimer.Enabled && API.getPlayerVehicleSeat(player) == -1)
                {
                    API.sendChatMessageToPlayer(player, Color.Yellow, "[TAXI] You have returned to your taxi and will no longer be taken off-duty.");
                    veh.RespawnTimer.Stop();
                }
            }

            //Check for passengers entering available cabs
            if (API.getPlayerVehicleSeat(player) != -1)
            {
                if (veh.Job.Type == JobManager.TaxiJob)
                {
                    if (veh.Driver == null)
                    {
                        API.sendChatMessageToPlayer(player, Color.Yellow, "[TAXI] This taxi currently has no driver.");
                        API.warpPlayerOutOfVehicle(player, vehicle);
                        return;
                    }

                    if (veh.Driver.TaxiPassenger == null && OnDutyDrivers.Contains(veh.Driver) && TaxiRequests.Contains(character))
                    {
                        /*if (!taxi_requests.Contains(character))
                        {
                            API.sendChatMessageToPlayer(player, Color.Yellow, "[TAXI] You must have an active taxi request to ride in a taxi. ( /requesttaxi )");
                            API.warpPlayerOutOfVehicle(player, vehicle);
                            return;
                        }

                        if (veh.driver.taxi_passenger != null)
                        {
                            API.sendChatMessageToPlayer(player, Color.Yellow, "[TAXI] This driver already has an active fare.");
                            return;
                        }*/

                       
                        veh.Driver.TaxiPassenger = character;
                        character.TaxiDriver = veh.Driver;
                        TaxiRequests.Remove(character);

                        TaxiPictureNotification(player, veh.Driver.rp_name() + " has accepted your taxi request.", subject: "~y~Request Accepted");
                        SendMessageToOnDutyDrivers(veh.Driver.rp_name() + " has accepted " + character.rp_name() + "'s taxi request.");
                        API.sendChatMessageToPlayer(player, "[TAXI] Please set a destination on your map and then type: /setdestination");

                        API.triggerClientEvent(player, "update_fare_display", veh.Driver.TaxiFare, 0, "");
                        API.triggerClientEvent(veh.Driver.Client, "update_fare_display", veh.Driver.TaxiFare, 0, "");
                    }
                    else if(veh.Driver.TaxiPassenger == character)
                    {
                        API.triggerClientEvent(player, "update_fare_display", veh.Driver.TaxiFare, 0, "");
                        API.triggerClientEvent(veh.Driver.Client, "update_fare_display", veh.Driver.TaxiFare, 0, "");

                        API.sendChatMessageToPlayer(player, "[TAXI] Please set a destination on your map and then type: /setdestination");
                    }
                }
            }
        }

        public void RespawnTaxi(Character character, Vehicle veh)
        {
            if (API.isPlayerConnected(character.Client))
            {
                API.sendChatMessageToPlayer(character.Client, Color.Yellow, "[TAXI] You were out of your taxi for too long and have taken off-duty. The taxi has been respawned.");

                if (OnDutyDrivers.Contains(character))
                {
                    OnDutyDrivers.Remove(character);
                }
                SendMessageToOnDutyDrivers(character.rp_name() + " has gone off of taxi duty.");
            }

            API.setVehicleEngineStatus(veh.NetHandle, false);
            veh.RespawnTimer.Stop();
            VehicleManager.respawn_vehicle(veh);
        }

        [Command("taxiduty")]
        public void taxiduty_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (character.JobOne.Type != JobManager.TaxiJob)
            {
                API.sendPictureNotificationToPlayer(player, "You must be a taxi driver to use this command.", "CHAR_BLOCKED", 0, 0, "Server", "~r~Command Error");
                return;
            }

            if (!API.isPlayerInAnyVehicle(player))
            {
                API.sendPictureNotificationToPlayer(player, "You must be in a taxi to use this command.", "CHAR_BLOCKED", 0, 0, "Server", "~r~Command Error");
                return;
            }

            if (API.getPlayerVehicleSeat(player) != -1)
            {
                API.sendPictureNotificationToPlayer(player, "You must be in the driver seat of a taxi to use this command.", "CHAR_BLOCKED", 0, 0, "Server", "~r~Command Error");
                return;
            }

            var veh = VehicleManager.GetVehFromNetHandle(API.getPlayerVehicle(player));

            if(veh.Job == null)
            {
                API.sendPictureNotificationToPlayer(player, "You must be driving a taxi car to go on taxi duty.", "CHAR_BLOCKED", 0, 0, "Server", "~r~Command Error");
                return;
            }

            if (veh.Job.Type != JobManager.TaxiJob)
            {
                API.sendPictureNotificationToPlayer(player, "You must be driving a taxi car to go on taxi duty.", "CHAR_BLOCKED", 0, 0, "Server", "~r~Command Error");
                return;
            }

            if (!OnDutyDrivers.Contains(character))
            {
                OnDutyDrivers.Add(character);
                SendMessageToOnDutyDrivers(character.rp_name() + " is now on taxi duty.");
                API.sendChatMessageToPlayer(player, Color.Yellow, "[TAXI] You are now on taxi duty. If you leave your vehicle you will be taken off duty automatically.");
            }
            else
            {
                OnDutyDrivers.Remove(character);
                SendMessageToOnDutyDrivers(character.rp_name() + " has gone off of taxi duty.");
                API.sendChatMessageToPlayer(player, Color.Yellow, "[TAXI] You have gone off of taxi duty.");
            }
        }

        [Command("setfare")]
        public void setfare_cmd(Client player, int farePrice)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if(character.JobOne.Type != JobManager.TaxiJob)
            {
                API.sendPictureNotificationToPlayer(player, "You must be a taxi driver to use this command.", "CHAR_BLOCKED", 0, 1, "Server", "~r~Command Error");
                return;
            }

            if(farePrice < MinFare || farePrice > MaxFare)
            {
                API.sendPictureNotificationToPlayer(player, "Your fair price must be between $" + MinFare + " and $" + MaxFare + ".", "CHAR_BLOCKED", 0, 1, "Server", "~r~Command Error");
                return;
            }

            if(character.TaxiPassenger != null)
            {
                API.sendPictureNotificationToPlayer(player, "You can't change your fare while you have a passenger!", "CHAR_BLOCKED", 0, 1, "Server", "~r~Command Error");
                return;
            }

            character.TaxiFare = farePrice;
            character.Save();
            API.sendChatMessageToPlayer(player, Color.Yellow, "[TAXI] You have changed your taxi fare to $" + farePrice + ".");
        }

        [Command("requesttaxi")]
        public void requesttaxi_cmd(Client player)
        {
            Character character = API.getEntityData(player.handle, "Character");

            if (TaxiRequests.Contains(character))
            {
                TaxiPictureNotification(player, "Our system shows you already have a taxi request submitted. If this is incorrect, use /canceltaxi");
                return;
            }

            if(character.TaxiDriver != null)
            {
                TaxiPictureNotification(player, "Our logs show you are already riding in a taxi.");
                return;
            }

            if(OnDutyDrivers.Count == 0)
            {
                TaxiPictureNotification(player, "Downtown Cab Co currently has no on duty drivers. Sorry for the inconvience.");
                return;
            }

            TaxiRequests.Add(character);
            TaxiPictureNotification(player, "Your taxi request has been submitted. Please wait patiently for a response.");
            
            foreach(var c in OnDutyDrivers)
            {
                TaxiPictureNotification(c.Client, character.rp_name() + " has requested a taxi. (" +  (c.Client.position.DistanceTo(character.Client.position) / 1000)+ "KM away) ((ID: " + PlayerManager.GetPlayerId(character) + "))");
            }
        }

        [Command("setdestination")]
        public void setdestination_cmd(Client player)
        {
            if (!API.isPlayerInAnyVehicle(player))
            {
                API.sendPictureNotificationToPlayer(player, "You must be inside a taxi to use this command.", "CHAR_BLOCKED", 0, 1, "Server", "~r~Command Error");
                return;
            }

            Character character = API.getEntityData(player.handle, "Character");
            var veh = VehicleManager.GetVehFromNetHandle(API.getPlayerVehicle(player));

            if(veh.Driver.TaxiPassenger != character)
            {
                API.sendPictureNotificationToPlayer(player, "You are not inside a taxi which has accepted your ride request.", "CHAR_BLOCKED", 0, 1, "Server", "~r~Command Error");
                return;
            }

            API.triggerClientEvent(player, "get_waypoint_position");

            character.TaxiStart = character.Client.position;

            character.TaxiTimer = new Timer {Interval = 2000};
            character.TaxiTimer.Elapsed += delegate { TaxiMeterTimer(character); };
            character.TaxiTimer.Start();
        }

        public void TaxiMeterTimer(Character c)
        {
            c.TotalFare = (int)Math.Round(c.Client.position.DistanceTo(c.TaxiStart) / 100) * c.TaxiDriver.TaxiFare;
            var fareMsg = "";

            if(c.TotalFare > c.Money)
            {
                c.TotalFare = c.Money;
                fareMsg = "(Client money maxed out)";
            }

            API.triggerClientEvent(c.Client, "update_fare_display", c.TaxiDriver.TaxiFare, c.TotalFare, fareMsg);
            API.triggerClientEvent(c.TaxiDriver.Client, "update_fare_display", c.TaxiDriver.TaxiFare, c.TotalFare, fareMsg);
        }

        [Command("acceptfare")]
        public void acceptfare_cmd(Client player, string id)
        {
            Character character = API.getEntityData(player.handle, "Character");
            if(character.JobOne.Type != JobManager.TaxiJob)
            {
                API.sendPictureNotificationToPlayer(player, "You must be a taxi driver to use this command.", "CHAR_BLOCKED", 0, 1, "Server", "~r~Command Error");
                return;
            }

            var passengerClient = PlayerManager.ParseClient(id);

            if (passengerClient == null)
            {
                API.sendNotificationToPlayer(player, "~r~ERROR:~w~ Invalid player entered.");
                return;
            }

            Character passenger = API.getEntityData(passengerClient.handle, "Character");

            if (!TaxiRequests.Contains(passenger))
            {
                TaxiPictureNotification(player, "This customer has already had their fare accepted or does not have an active fare.");
                return;
            }

            character.TaxiPassenger = passenger;
            passenger.TaxiDriver = character;
            TaxiRequests.Remove(passenger);

            TaxiPictureNotification(passenger.Client, character.rp_name() + " has accepted your taxi request. Please stay at your current location.");
            TaxiPictureNotification(player, "You have accepted " + passenger.rp_name() + "'s taxi request. Follow your waypoint to their location.");

            API.triggerClientEvent(player, "set_taxi_waypoint", passenger.Client.position);
        }

        public static bool IsOnTaxiDuty(Character c)
        {
            return OnDutyDrivers.Contains(c);
        }

        public void TaxiPictureNotification(Client player, string body, string sender = "Downton Cab Co", string subject = "Dispatch Message")
        {
            API.sendPictureNotificationToPlayer(player, body, "CHAR_TAXI", 0, 1, sender, subject);
        }

        public void SendMessageToOnDutyDrivers(string body, string sender = "Downtown Cab Co", string subject = "Dispatch Message")
        {
            foreach(var c in OnDutyDrivers)
            {
                TaxiPictureNotification(c.Client, body, sender, subject);
            }
        }
    }
}
