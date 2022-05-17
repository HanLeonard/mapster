using Mapster.Common.MemoryMappedTypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Mapster.Rendering;

public static class TileRenderer {
    public static BaseShape Tessellate(this MapFeatureData feature, ref BoundingBox boundingBox,
        ref PriorityQueue<BaseShape, int> shapes) {
        BaseShape? baseShape = null;

        var featureType = feature.Type;
        // OWN-CODE
        // We replaced the string comparisons of properties with int comparisons and multiple iterations through all properties with only one*
        // (still multiple if you count the border/ populatedPlace method, which were not refactored to maintain readability and portability)

        if (Border.ShouldBeBorder(feature)) {
            var coordinates = feature.Coordinates;
            var border = new Border(coordinates);
            baseShape = border;
            shapes.Enqueue(border, border.ZIndex);
        }
        else if (PopulatedPlace.ShouldBePopulatedPlace(feature)) {
            var coordinates = feature.Coordinates;
            var popPlace = new PopulatedPlace(coordinates, feature);
            baseShape = popPlace;
            shapes.Enqueue(popPlace, popPlace.ZIndex);
        }
        else {
            // Use variable to exit for after one case
            bool found = false;
            foreach (var el in feature.Properties) {
                if (found)
                    break;
                ReadOnlySpan<Coordinate> coordinates;
                switch (el.Key) {
                    case MapFeatureData.CustomPropertyEnum.Highway:
                        if (MapFeature.HighwayTypes.Any(v => el.Value.StartsWith(v))) {
                            found = true;
                            coordinates = feature.Coordinates;
                            var road = new Road(coordinates);
                            baseShape = road;
                            shapes.Enqueue(road, road.ZIndex);
                        }

                        break;
                    case MapFeatureData.CustomPropertyEnum.Water:
                        if (feature.Type != GeometryType.Point) {
                            found = true;
                            coordinates = feature.Coordinates;

                            var waterway = new Waterway(coordinates, feature.Type == GeometryType.Polygon);
                            baseShape = waterway;
                            shapes.Enqueue(waterway, waterway.ZIndex);
                        }

                        break;
                    case MapFeatureData.CustomPropertyEnum.Railway:
                        found = true;
                        coordinates = feature.Coordinates;
                        var railway = new Railway(coordinates);
                        baseShape = railway;
                        shapes.Enqueue(railway, railway.ZIndex);
                        break;
                    case MapFeatureData.CustomPropertyEnum.Natural:
                        if (featureType == GeometryType.Polygon) {
                            found = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, feature);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        }

                        break;
                    case MapFeatureData.CustomPropertyEnum.Boundary:
                        if (el.Value.StartsWith("forest")) {
                            found = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Forest);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        }

                        break;
                    case MapFeatureData.CustomPropertyEnum.Landuse:
                        if (el.Value.StartsWith("forest") || el.Value.StartsWith("orchard")) {
                            found = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Forest);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        } else if (feature.Type == GeometryType.Polygon) {
                            if (el.Value.StartsWith("residential") || el.Value.StartsWith("cemetery") ||
                                el.Value.StartsWith("industrial") || el.Value.StartsWith("commercial") ||
                                el.Value.StartsWith("square") || el.Value.StartsWith("construction") ||
                                el.Value.StartsWith("military") || el.Value.StartsWith("quarry") ||
                                el.Value.StartsWith("brownfield")) {
                                found = true;
                                coordinates = feature.Coordinates;
                                var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
                                baseShape = geoFeature;
                                shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                            }else if (el.Value.StartsWith("farm") || el.Value.StartsWith("meadow") ||
                                      el.Value.StartsWith("grass") || el.Value.StartsWith("greenfield") ||
                                      el.Value.StartsWith("recreation_ground") || el.Value.StartsWith("winter_sports")
                                      || el.Value.StartsWith("allotments")) {
                                found = true;
                                coordinates = feature.Coordinates;
                                var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Plain);
                                baseShape = geoFeature;
                                shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                            } else if (el.Value.StartsWith("reservoir") || el.Value.StartsWith("basin")) {
                                found = true;
                                coordinates = feature.Coordinates;
                                var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Water);
                                baseShape = geoFeature;
                                shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                            }
                        }

                        break;
                    case MapFeatureData.CustomPropertyEnum.Building:
                        if (feature.Type == GeometryType.Polygon) {
                            found = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        }
                        break;
                    case MapFeatureData.CustomPropertyEnum.Leisure:
                        if (feature.Type == GeometryType.Polygon) {
                            found = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        }
                        break;
                    case MapFeatureData.CustomPropertyEnum.Amenity:
                        if (feature.Type == GeometryType.Polygon) {
                            found = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        }

                        break;
                    default:
                        break;
                }
            }
        }

        if (baseShape != null) {
            for (var j = 0; j < baseShape.ScreenCoordinates.Length; ++j) {
                boundingBox.MinX = Math.Min(boundingBox.MinX, baseShape.ScreenCoordinates[j].X);
                boundingBox.MaxX = Math.Max(boundingBox.MaxX, baseShape.ScreenCoordinates[j].X);
                boundingBox.MinY = Math.Min(boundingBox.MinY, baseShape.ScreenCoordinates[j].Y);
                boundingBox.MaxY = Math.Max(boundingBox.MaxY, baseShape.ScreenCoordinates[j].Y);
            }
        }

        return baseShape;
    }

    public static Image<Rgba32> Render(this PriorityQueue<BaseShape, int> shapes, BoundingBox boundingBox, int width,
        int height) {
        var canvas = new Image<Rgba32>(width, height);

        // Calculate the scale for each pixel, essentially applying a normalization
        var scaleX = canvas.Width / (boundingBox.MaxX - boundingBox.MinX);
        var scaleY = canvas.Height / (boundingBox.MaxY - boundingBox.MinY);
        var scale = Math.Min(scaleX, scaleY);

        // Background Fill
        canvas.Mutate(x => x.Fill(Color.White));
        while (shapes.Count > 0) {
            var entry = shapes.Dequeue();
            entry.TranslateAndScale(boundingBox.MinX, boundingBox.MinY, scale, canvas.Height);
            canvas.Mutate(x => entry.Render(x));
        }

        return canvas;
    }

    public struct BoundingBox {
        public float MinX;
        public float MaxX;
        public float MinY;
        public float MaxY;
    }
}