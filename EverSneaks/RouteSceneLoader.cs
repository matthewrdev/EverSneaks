using System;
using System.Collections.Generic;
using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Evergine.Components.Graphics3D;
using Evergine.Components.Primitives;
using Redpoint.Infrastructure.Utilities;

namespace EverSneaks;

public static class RouteSceneLoader
{
    public static void LoadSceneFromJson(DefaultScene scene,
                                        Application application,
                                        Color defaultRouteColor)
    {
        var json =  ResourcesHelper.ReadResourceTextContent(typeof(RouteSceneLoader).Assembly, "BlueMountains");
        var routeData = JsonSerializer.Deserialize<RouteData>(json);


        // 2. Apply camera setup (optional)
        if (!string.IsNullOrWhiteSpace(routeData.CameraPosition) &&
            !string.IsNullOrWhiteSpace(routeData.CameraRotation))
        {
            var cc  =scene.Managers.EntityManager.FindFirstComponentOfType<Camera3D>();
            
            cc.Transform.Position = ParseVector3(routeData.CameraPosition);
            cc.Transform.LocalOrientation = ParseQuaternion(routeData.CameraRotation);
        }

        // 3. Load route line meshes and labels
        ParseRoutesWithLineColor(scene, defaultRouteColor, routeData);
    }

    private static void ParseRoutesWithLineColor(DefaultScene scene, 
                                                 Color defaultRouteColor,
                                                 RouteData routeData)
    {
        foreach (var route in routeData.Routes)
        {
            var points = ParsePath(route.Path, ParseVector3);
            var color = TryParseColor(route.Color, defaultRouteColor);

            var routeEntity = CreateRouteLine(points, color);

            scene.Managers.EntityManager.Add(routeEntity);
        }
    }

    private static Entity CreateRouteLine(Vector3[] points, Color color)
    {
        var lineMesh = new LineMesh();

        lineMesh.LineType = LineType.LineStrip;
        lineMesh.IsLoop = false;

        List<LinePointInfo> linePoints = new List<LinePointInfo>();
        for (int i = 0; i < points.Length; i++)
        {
            var point = points[i];
            
            linePoints.Add(new LinePointInfo()
            {                Position = point,
                Color = color,
                Thickness = 0.5f
            });
        }

        lineMesh.IsCameraAligned = true;
        lineMesh.LinePoints = linePoints;

        return new Entity("RouteLine")
            .AddComponent(new Transform3D())
            .AddComponent(lineMesh)
            .AddComponent(new LineMeshRenderer3D()
            {
                CastShadows = false,
            });
    }

    private static Vector3[] ParsePath(string path, Func<string, Vector3> parseVector3)
    {
        return path.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(parseVector3).ToArray();
    }
    
    private static Vector3 ParseVector3(string csv)
    {
        var parts = csv.Split(',');
        return new Vector3(
            -float.Parse(parts[0], CultureInfo.InvariantCulture),
            float.Parse(parts[1], CultureInfo.InvariantCulture),
            float.Parse(parts[2], CultureInfo.InvariantCulture));
    }

    private static Quaternion ParseQuaternion(string csv)
    {
        var parts = csv.Split(',');
        return new Quaternion(
            float.Parse(parts[0], CultureInfo.InvariantCulture),
            float.Parse(parts[1], CultureInfo.InvariantCulture),
            float.Parse(parts[2], CultureInfo.InvariantCulture),
            float.Parse(parts[3], CultureInfo.InvariantCulture));
    }

    private static Color TryParseColor(string hex, Color fallback)
    {
        if (!string.IsNullOrWhiteSpace(hex) && hex.Length == 6 &&
            byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, null, out var r) &&
            byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, null, out var g) &&
            byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, null, out var b))
        {
            return new Color(r, g, b, 255);
        }

        return fallback;
    }

    // JSON DTOs
    private class RouteData
    {
        public string AssetUrl { get; set; }
        public string AssetScale { get; set; }
        public string AssetRotation { get; set; }
        public string CameraPosition { get; set; }
        public string CameraRotation { get; set; }
        public List<Route> Routes { get; set; }
    }

    private class Route
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Color { get; set; }
        public string LabelPosition { get; set; }
    }
}
