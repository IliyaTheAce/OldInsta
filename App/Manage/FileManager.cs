﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace Insta_DM_Bot_server_wpf
{
    internal static class FileManager
    {
        /// <summary>
        /// Writes the given object instance to a binary file.
        /// <para>Object type (and all child types) must be decorated with the [Serializable] attribute.</para>
        /// <para>To prevent a variable from being serialized, decorate it with the [NonSerialized] attribute; cannot be applied to properties.</para>
        /// </summary>
        /// <typeparam name="T">The type of object being written to the XML file.</typeparam>
        /// <param name="filePath">The file path to write the object instance to.</param>
        /// <param name="objectToWrite">The object instance to write to the XML file.</param>
        /// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
        public static void WriteToBinaryFile<T>(string filePath, T objectToWrite, bool append = false)
        {
            using (Stream stream = File.Open(filePath, append ? FileMode.Append : FileMode.Create))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, objectToWrite);
            }
        }

        /// <summary>
        /// Reads an object instance from a binary file.
        /// </summary>
        /// <typeparam name="T">The type of object to read from the XML.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the binary file.</returns>
        public static T ReadFromBinaryFile<T>(string filePath)
        {
            if (!Directory.Exists("./App/"))
            {
                Directory.CreateDirectory("./App/");
            }
            if (!File.Exists(filePath))
            {
                File.Create(filePath);
            }
            using (Stream stream = File.Open(filePath, FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (T)binaryFormatter.Deserialize(stream);
            }
        }

        public static void CheckFilesAndDirectories()
        {
            if (!File.Exists("./App/Log/Log.json"))
            {
                if (!Directory.Exists("./App/Log"))
                {
                    Directory.CreateDirectory("./App/Log");
                }
                File.Create("./App/Log/Log.json");
            }


        }
        
        public class Credential
        {
            public string serverId;
        }

        // public static void SaveCredential(string location, string ip, string clientName)
        // {
        //     var credential = new Credential()
        //         { registered = true, location = location, ip = ip, clientName = clientName };
        //     File.WriteAllText("./App/Credential.json", JsonConvert.SerializeObject(credential));
        // }

        public static Credential? ReadCredential()
        {
            if (!File.Exists("./App/Credential.json"))
            {
                var credential = new Credential() { serverId = "0"};
                File.WriteAllText("./App/Credential.json",
                    JsonConvert.SerializeObject(credential));
                return credential;
            }
            else
            {
                var text = File.ReadAllText("./App/Credential.json");
                return JsonConvert.DeserializeObject<Credential>(text);
            }
        }
    }
}
