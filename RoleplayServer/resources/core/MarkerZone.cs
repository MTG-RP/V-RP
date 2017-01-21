﻿using GTANetworkServer;
using GTANetworkShared;
using MongoDB.Bson.Serialization.Attributes;

namespace RoleplayServer.resources.core
{
    public class MarkerZone
    {
        public string LabelText { get; set; }

        public Vector3 Location { get; set; }
        public Vector3 Rotation { get; set; }
        public int Dimension { get; set; }

        public float ColZoneSize { get; set; }

        public int MarkerType { get; set; }
        public Vector3 Scale { get; set; }
        public int Alpha { get; set; }
        public int Red { get; set; }
        public int Blue { get; set; }
        public int Green { get; set; }

        public int BlipSprite { get; set; }

        [BsonIgnore]
        public NetHandle Marker { get; set; }
        [BsonIgnore]
        public NetHandle Label { get; set; }
        [BsonIgnore]
        public NetHandle Blip { get; set; }
        [BsonIgnore]
        public Rectangle2DColShape ColZone { get; set; }

        public MarkerZone(Vector3 loc, Vector3 rot, int dimension = 0, float zoneSize = 10.0f)
        {
            Location = loc;
            Rotation = rot;
            Dimension = dimension;
            ColZoneSize = zoneSize;


            MarkerType = 2;
            Alpha = 255;
            Red = 255;
            Green = 255;
            Blue = 0;

            Scale = new Vector3(0.5, 0.5, 0.5);
        }

        public void Create()
        {
            Marker = API.shared.createMarker(2, Location, Location, Rotation, Scale, Alpha, Red, Green, Blue, Dimension);
            Label = API.shared.createTextLabel("~g~" + LabelText, Location.Add(new Vector3(0.0, 0.0, 0.5)), 25f, 0.5f, true, Dimension);
            ColZone = API.shared.create2DColShape(Location.X, Location.Y, ColZoneSize, 5.0f);

            Blip = API.shared.createBlip(Marker);
            API.shared.setBlipSprite(Blip, BlipSprite);
        }
    }
}
