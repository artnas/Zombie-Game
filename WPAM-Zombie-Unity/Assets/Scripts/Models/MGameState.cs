using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace DefaultNamespace.Models
{
    [System.Serializable]
    public class MGameState
    {
        public DateTime DateTime;
        public MPlayer Player;
        public MHideout Hideout;
        public List<MOutpost> Outposts = new List<MOutpost>();
        
        public void SerializeToFile(string filePath)
        {
            DateTime = DateTime.Now;

            var serializer = new XmlSerializer(typeof(MGameState));
            
            TextWriter writer = new StringWriter();
            serializer.Serialize(writer, this);
            
            File.WriteAllText(filePath, writer.ToString());
        }

        public static MGameState DeserializeFromFile(string filePath)
        {
            var serializer = new XmlSerializer(typeof(MGameState));
            var text = File.ReadAllText(filePath);
            
            TextReader reader = new StringReader(text);

            return (MGameState)serializer.Deserialize(reader);
        }
    }
}