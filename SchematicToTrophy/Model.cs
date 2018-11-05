using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchematicToTrophy
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
                    "head", new Dictionary<string, double[]>()
                    {
                        { "rotation", new double[] { -30, 0, 0 } },
                        { "translation", new double[] { 0, -30.75, -7.25 } },
                        { "scale", new double[] { 3.0125, 3.0125, 3.0125 } }
                    }
                }
            };
        }
    }
}
