using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftSchematicTo3DModel
{
    class Model
    {
        public Dictionary<string, object> display;
        public Dictionary<string, string> textures;
        public List<Dictionary<string, object>> elements;

        public Model(Dictionary<string, string> textures, List<Dictionary<string, object>> elements)
        {
            this.textures = textures;
            this.elements = elements;

            this.display = new Dictionary<string, object>()
            {
                {
                    "fixed", new Dictionary<string, double[]>()
                    {
                        { "rotation", new double[] { 0, -180, 0 } },
                        { "translation", new double[] { 0, 0, -16 } },
                        { "scale", new double[] { 2.001, 2.001, 2.001 } }
                    }
                }
            };
        }
    }
}
