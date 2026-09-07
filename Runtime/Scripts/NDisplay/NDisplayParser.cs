using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
// using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mox
{
    namespace NDisplay
    {
        public class NDisplayParser
        {
            #region NDISPLAYDATASTRUCTS
            public class CameraData
            {
                public string name = "";
                public Vector3 position = Vector3.zero;
                public Vector3 orientation = Vector3.zero;
            }

            public class ScreenData
            {
                public string name = "";
                public string parentId = "";
                public Vector3 position = Vector3.zero;
                public Vector3 orientation = Vector3.zero;
                public Vector2 size = Vector2.zero;
            }

            public class TransformData
            {
                public string name = "";
                public string parentId = "";
                public Vector3 position = Vector3.zero;
                public Vector3 orientation = Vector3.zero;
            };

            public class ViewportData
            {
                public string name = "";
                public string cameraName = "";
                public string screenName = "";
                public string meshName = "";
            }

            public class NodeData
            {
                public string name = "";
                public string primaryId = "";
                public string ip = "";
                public List<ViewportData> viewports = new List<ViewportData>();
                public Vector2 position = Vector2.zero;
                public Vector2 size = Vector2.zero;
            }

            public class NDisplayConfig
            {
                public List<CameraData> _cameraData = new List<CameraData>();
                public List<ScreenData> _screenData = new List<ScreenData>();
                public List<TransformData> _transformData = new List<TransformData>();
                public List<NodeData> _nodeData = new List<NodeData>();
            }
            #endregion

            #region PARSING
            public static NDisplayConfig ParseNDisplayConfig(string configPath)
            {
                NDisplayConfig result = new NDisplayConfig();

                if (File.Exists(configPath))
                {
                    StreamReader streamReader = new StreamReader(configPath);
                    string sJson = streamReader.ReadToEnd();

                    result = ParseConfig(sJson);
                }
                else if (File.Exists(UnityEngine.Application.streamingAssetsPath + "/" + configPath))
                {
                    StreamReader streamReader = new StreamReader(UnityEngine.Application.streamingAssetsPath + "/" + configPath);
                    string sJson = streamReader.ReadToEnd();

                    result = ParseConfig(sJson);
                }
                else
                {
                    Debug.LogError("Cannot find nDisplay config with given path \"" + configPath + "\"");
                }

                return result;
            }

            private static NDisplayConfig ParseConfig(string config)
            {
                NDisplayConfig result = new NDisplayConfig();

                try
                {
                    JObject root = JObject.Parse(config);

                    result._cameraData = ParseCameras(root);
                    result._screenData = ParseScreens(root);
                    result._transformData = ParseTransforms(root);
                    result._nodeData = ParseNodes(root);
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to parse json string from ndisplay file - " + e.Message);
                }

                return result;
            }

            private static List<CameraData> ParseCameras(JObject root)
            {
                List<CameraData> result = new List<CameraData>();

                try
                {
                    JObject jCameras = (JObject)root["nDisplay"]["scene"]["cameras"];

                    if (jCameras != null)
                    {
                        foreach (JToken camera in jCameras.Children())
                        {
                            CameraData data = new CameraData();

                            JProperty jCamera = (JProperty)camera;

                            data.name = jCamera.Name;

                            foreach (JToken cameraValue in camera.Values())
                            {
                                JProperty jValue = (JProperty)cameraValue;
                                if (jValue.Name == "location")
                                {
                                    JObject jLocation = (JObject)jValue.Value;

                                    float x = jLocation["x"].Value<float>();
                                    float y = jLocation["y"].Value<float>();
                                    float z = jLocation["z"].Value<float>();

                                    data.position = new Vector3(x, y, z);
                                }
                                else if (jValue.Name == "rotation")
                                {
                                    JObject jRotation = (JObject)jValue.Value;

                                    float roll = jRotation["roll"].Value<float>();
                                    float pitch = jRotation["pitch"].Value<float>();
                                    float yaw = jRotation["yaw"].Value<float>();

                                    data.orientation = new Vector3(roll, pitch, yaw);
                                }
                            }

                            result.Add(data);
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to find cameras node in nDisplay config");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to parse camera nodes - " + e.Message);
                }

                return result;
            }

            private static List<ScreenData> ParseScreens(JObject root)
            {
                List<ScreenData> result = new List<ScreenData>();

                try
                {
                    JObject jScreens = (JObject)root["nDisplay"]["scene"]["screens"];

                    if (jScreens != null)
                    {
                        foreach (JToken screen in jScreens.Children())
                        {
                            ScreenData data = new ScreenData();

                            JProperty jScreen = (JProperty)screen;

                            data.name = jScreen.Name;

                            foreach (JToken screenValue in screen.Values())
                            {
                                JProperty jValue = (JProperty)screenValue;

                                if (jValue.Name == "parentId")
                                {
                                    JToken val = jValue.Value;
                                    data.parentId = val.Value<string>();
                                }
                                else if (jValue.Name == "location")
                                {
                                    JObject jLocation = (JObject)jValue.Value;

                                    float x = jLocation["x"].Value<float>();
                                    float y = jLocation["y"].Value<float>();
                                    float z = jLocation["z"].Value<float>();

                                    data.position = new Vector3(x, y, z);
                                }
                                else if (jValue.Name == "rotation")
                                {
                                    JObject jRotation = (JObject)jValue.Value;

                                    float roll = jRotation["roll"].Value<float>();
                                    float pitch = jRotation["pitch"].Value<float>();
                                    float yaw = jRotation["yaw"].Value<float>();

                                    data.orientation = new Vector3(roll, pitch, yaw);
                                }
                                else if (jValue.Name == "size")
                                {
                                    JObject jSize = (JObject)jValue.Value;

                                    float width = jSize["width"].Value<float>();
                                    float height = jSize["height"].Value<float>();

                                    data.size = new Vector2(width, height);
                                }
                            }

                            result.Add(data);
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to find screens node in nDisplay config");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to parse screen nodes - " + e.Message);
                }

                return result;
            }

            private static List<TransformData> ParseTransforms(JObject root)
            {
                List<TransformData> result = new List<TransformData>();

                try
                {
                    JObject jTransforms = (JObject)root["nDisplay"]["scene"]["xforms"];

                    if (jTransforms != null)
                    {
                        foreach (JToken transform in jTransforms.Children())
                        {
                            TransformData data = new TransformData();

                            JProperty jTransform = (JProperty)transform;
                            data.name = jTransform.Name;

                            foreach (JToken transformValue in transform.Values())
                            {
                                JProperty jValue = (JProperty)transformValue;
                                if (jValue.Name == "parentId")
                                {
                                    JToken val = jValue.Value;
                                    data.parentId = val.Value<string>();
                                }
                                else if (jValue.Name == "location")
                                {
                                    JObject jLocation = (JObject)jValue.Value;

                                    float x = jLocation["x"].Value<float>();
                                    float y = jLocation["y"].Value<float>();
                                    float z = jLocation["z"].Value<float>();

                                    data.position = new Vector3(x, y, z);
                                }
                                else if (jValue.Name == "rotation")
                                {
                                    JObject jRotation = (JObject)jValue.Value;

                                    float roll = jRotation["roll"].Value<float>();
                                    float pitch = jRotation["pitch"].Value<float>();
                                    float yaw = jRotation["yaw"].Value<float>();

                                    data.orientation = new Vector3(roll, pitch, yaw);
                                }
                            }

                            result.Add(data);
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to find xforms node in nDisplay config");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to parse transform nodes - " + e.Message);
                }

                return result;
            }


            private static List<NodeData> ParseNodes(JObject root)
            {
                List<NodeData> result = new List<NodeData>();

                try
                {
                    var jPrimaryId = root["nDisplay"]["cluster"]["primaryNode"]["id"];
                    string primaryId = jPrimaryId.Value<string>();

                    JObject jNodes = (JObject)root["nDisplay"]["cluster"]["nodes"];

                    foreach (JToken node in jNodes.Children())
                    {
                        NodeData data = new NodeData();
                        data.primaryId = primaryId;

                        JProperty jNode = (JProperty)node;

                        data.name = jNode.Name;

                        foreach (JToken screenValue in node.Values())
                        {
                            JProperty jValue = (JProperty)screenValue;
                            if (jValue.Name == "host")
                            {
                                JToken val = jValue.Value;

                                data.ip = val.Value<string>();
                            }
                            else if (jValue.Name == "viewports")
                            {
                                JObject jViewports = (JObject)jValue.Value;

                                ParseViewports(jViewports, ref data);
                            }
                            else if (jValue.Name == "window")
                            {
                                JObject jWindow = (JObject)jValue.Value;

                                float x = jWindow["x"].Value<float>();
                                float y = jWindow["y"].Value<float>();
                                float w = jWindow["w"].Value<float>();
                                float h = jWindow["h"].Value<float>();

                                data.position = new Vector2(x, y);
                                data.size = new Vector2(w, h);
                            }
                        }

                        result.Add(data);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to parse viewport nodes - " + e.Message);
                }

                return result;
            }

            private static void ParseViewports(JObject node, ref NodeData data)
            {
                List<ViewportData> viewports = new List<ViewportData>();

                foreach (JToken viewport in node.Children())
                {
                    ViewportData viewportData = new ViewportData();

                    viewportData.name = ((JProperty)viewport).Name;

                    viewports.Add(viewportData);
                }

                int idx = 0;


                foreach (JToken viewportValue in node.Values())
                {
                    ViewportData viewportData = viewports[idx];

                    foreach (JToken paramValue in viewportValue.Children())
                    {
                        JProperty prop = (JProperty)paramValue;

                        if (prop.Name == "camera")
                        {
                            JToken val = prop.Value;

                            viewportData.cameraName = val.Value<string>();

                            if (viewportData.cameraName.Length <= 0)
                            {
                                viewportData.cameraName = "DefaultViewPoint";
                            }
                        }
                        else if (prop.Name == "projectionPolicy")
                        {
                            // JObject projectionObject = (JObject)paramValue;

                            foreach (JToken projectionValue in prop.Values())
                            {
                                JProperty projProp = (JProperty)projectionValue;

                                if (projProp.Name == "parameters")
                                {
                                    foreach (JToken pValue in projProp.Values())
                                    {
                                        JProperty pProp = (JProperty)pValue;

                                        if (pProp.Name == "screen")
                                        {
                                            JToken snToken = pProp.Value;

                                            viewportData.screenName = snToken.Value<string>();
                                        }
                                        else if (pProp.Name == "mesh_component")
                                        {
                                            JToken meshToken = pProp.Value;

                                            viewportData.meshName = meshToken.Value<string>();
                                        }
                                    }
                                }
                            }
                        }
                    }

                    viewports[idx] = viewportData;

                    ++idx;
                }

                data.viewports = viewports;

                //foreach (JToken viewport in node.Children())
                //{
                //    // 

                //    // JToken viewportValue = viewport.Values();

                //    foreach(JToken foo in node.Values())
                //    {
                //        int bar = 0;
                //    }

                //    foreach (JToken viewportValue in node.Values())
                //    {

                //        ViewportData viewportData = new ViewportData();

                //        viewportData.name = ((JProperty)viewportValue).Name;

                //        foreach (JToken paramValue in viewportValue.Children())
                //        {
                //            JProperty prop = (JProperty)paramValue;

                //            if (prop.Name == "camera")
                //            {
                //                JToken val = prop.Value;

                //                viewportData.cameraName = val.Value<string>();

                //                if(viewportData.cameraName.Length <= 0)
                //                {
                //                    viewportData.cameraName = "DefaultViewPoint";
                //                }
                //            }
                //            else if (prop.Name == "projectionPolicy")
                //            {
                //                // JObject projectionObject = (JObject)paramValue;

                //                foreach (JToken projectionValue in prop.Values())
                //                {
                //                    JProperty projProp = (JProperty)projectionValue;

                //                    if (projProp.Name == "parameters")
                //                    {
                //                        foreach (JToken pValue in projProp.Values())
                //                        {
                //                            JProperty pProp = (JProperty)pValue;

                //                            if (pProp.Name == "screen")
                //                            {
                //                                JToken snToken = pProp.Value;

                //                                viewportData.screenName = snToken.Value<string>();
                //                            }
                //                            else if(pProp.Name == "mesh_component")
                //                            {
                //                                JToken meshToken = pProp.Value;

                //                                viewportData.meshName = meshToken.Value<string>();
                //                            }
                //                        }
                //                    }
                //                }
                //            }
                //        }

                //        data.viewports.Add(viewportData);
                //    }            
                //}
            }

            #endregion
        }
    }
}

