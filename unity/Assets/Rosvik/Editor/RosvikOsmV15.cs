#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using UnityEngine;

namespace Rosvik.Blackout.EditorTools {
    public static class RosvikOsmV15 {
        const double OriginLat = 65.42707;
        const double OriginLon = 21.69108;
        const double MPerLat = 111320.0;
        static readonly double MPerLon = 111320.0 * Math.Cos(OriginLat * Math.PI / 180.0);

        // Wider capture around the school/sports complex. The earlier V15 bbox was
        // intentionally tight, but it made the world read like a cropped test plot.
        // V16 keeps the same metre origin but includes Rosvalla, the ice arena,
        // surrounding roads, houses and more of the village fabric.
        const string Url = "https://api.openstreetmap.org/api/0.6/map?bbox=21.6845,65.4215,21.7025,65.4325";
        const string Cache = "Library/RosvikMapV16.osm";

        public class Node { public long Id; public Vector3 Pos; }
        public class Way {
            public long Id;
            public List<Node> Nodes = new List<Node>();
            public Dictionary<string,string> Tags = new Dictionary<string,string>();
            public string Tag(string key) => Tags.TryGetValue(key, out string v) ? v : null;
            public bool Closed => Nodes.Count > 2 && Nodes[0].Id == Nodes[Nodes.Count-1].Id;
        }
        public struct OBounds { public Vector3 Center, AxisX; public float Width, Depth; }

        public static List<Way> LoadWays() {
            string xml = LoadXml();
            if (string.IsNullOrEmpty(xml)) return null;
            XDocument doc = XDocument.Parse(xml);
            var nodes = new Dictionary<long,Node>();
            foreach (XElement e in doc.Root.Elements("node")) {
                long id = long.Parse(e.Attribute("id").Value, CultureInfo.InvariantCulture);
                double lat = double.Parse(e.Attribute("lat").Value, CultureInfo.InvariantCulture);
                double lon = double.Parse(e.Attribute("lon").Value, CultureInfo.InvariantCulture);
                nodes[id] = new Node { Id=id, Pos=ToLocal(lat,lon) };
            }
            var ways = new List<Way>();
            foreach (XElement e in doc.Root.Elements("way")) {
                var w = new Way { Id=long.Parse(e.Attribute("id").Value, CultureInfo.InvariantCulture) };
                foreach (XElement nd in e.Elements("nd")) {
                    long id = long.Parse(nd.Attribute("ref").Value, CultureInfo.InvariantCulture);
                    if (nodes.TryGetValue(id, out Node n)) w.Nodes.Add(n);
                }
                foreach (XElement t in e.Elements("tag")) w.Tags[t.Attribute("k").Value] = t.Attribute("v").Value;
                if (w.Nodes.Count >= 2) ways.Add(w);
            }
            return ways;
        }

        static string LoadXml() {
            try {
                using (var wc = new WebClient()) {
                    wc.Headers.Add("User-Agent", "RosvikBlackoutUnityEditor/1.0");
                    string xml = wc.DownloadString(Url);
                    File.WriteAllText(Cache, xml);
                    return xml;
                }
            } catch (Exception ex) {
                Debug.LogWarning("ROSVIK OSM download failed: " + ex.Message);
                return File.Exists(Cache) ? File.ReadAllText(Cache) : null;
            }
        }

        static Vector3 ToLocal(double lat, double lon) {
            return new Vector3((float)((lon-OriginLon)*MPerLon), 0f, (float)((lat-OriginLat)*MPerLat));
        }

        public static OBounds Bounds(Way w) {
            List<Vector3> pts = w.Nodes.Select(n=>n.Pos).ToList();
            if (w.Closed) pts.RemoveAt(pts.Count-1);
            Vector3 c=Vector3.zero; foreach(Vector3 p in pts)c+=p; c/=Mathf.Max(1,pts.Count);
            Vector3 axis=Vector3.right; float best=0f;
            for(int i=0;i<pts.Count;i++) {
                Vector3 d=pts[(i+1)%pts.Count]-pts[i]; d.y=0f;
                if(d.sqrMagnitude>best){best=d.sqrMagnitude; axis=d.normalized;}
            }
            Vector3 perp=new Vector3(-axis.z,0f,axis.x);
            float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;
            foreach(Vector3 p in pts){
                Vector3 q=p-c; float x=Vector3.Dot(q,axis), z=Vector3.Dot(q,perp);
                minX=Mathf.Min(minX,x); maxX=Mathf.Max(maxX,x); minZ=Mathf.Min(minZ,z); maxZ=Mathf.Max(maxZ,z);
            }
            return new OBounds { Center=c, AxisX=axis, Width=maxX-minX, Depth=maxZ-minZ };
        }

        public static Vector3 Centroid(Way w) {
            Vector3 c=Vector3.zero; foreach(Node n in w.Nodes)c+=n.Pos; return c/Mathf.Max(1,w.Nodes.Count);
        }
    }
}
#endif
