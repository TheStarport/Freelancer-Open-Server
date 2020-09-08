/*
 * Matt Dean 2011
 * Adapted from LuaParse.cs by Denis Bekman 2009 www.youpvp.com/blog
 --
 * This code is licensed under a Creative Commons Attribution 3.0 United States License.
 * To view a copy of this license, visit http://creativecommons.org/licenses/by/3.0/us/
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FLServer
{
    public class ThnParse
    {
        private readonly List<string> toks = new List<string>();

        public List<Entity> entities = new List<Entity>();

        protected bool IsLiteral
        {
            get { return Regex.IsMatch(toks[0], "^[a-zA-Z]+[0-9a-zA-Z_]*"); }
        }

        protected bool IsString
        {
            get { return Regex.IsMatch(toks[0], "^\"([^\"]*)\""); }
        }

        protected bool IsNumber
        {
            get { return Regex.IsMatch(toks[0], @"^\d+"); }
        }

        protected bool IsFloat
        {
            get { return Regex.IsMatch(toks[0], @"^\d*\.\d+"); }
        }

        public void Parse(string s)
        {
            string qs = string.Format("({0}[^{0}]*{0})", "\"");
            string[] z = Regex.Split(s, qs + @"|(=)|(,)|(\[)|(\])|(\{)|(\})|(--[^\n\r]*)|(\r\n\r\n)|;");

            foreach (string tok in z)
            {
                if (tok.Trim().Length != 0 && !tok.StartsWith("--"))
                {
                    toks.Add(tok.Trim());
                }
            }

            // read the duration
            if (!IsToken("duration"))
                throw new Exception("expect 'duration'");
            NextToken();
            if (!IsToken("="))
                throw new Exception("expect '='");
            NextToken();
            float duration = GetFloat();

            // Read the entities block
            if (!IsToken("entities"))
                throw new Exception("expect 'duration'");
            NextToken();
            if (!IsToken("="))
                throw new Exception("expect '='");
            NextToken();
            if (!IsToken("{"))
                throw new Exception("expect '{'");
            NextToken();
            ReadEntitiesSection();

            // Read the events block
        }

        private Vector ReadVectorBlock()
        {
            var v = new Vector();
            if (GetToken() != "{")
                throw new Exception("expecting '{': tok = " + toks[0]);
            v.x = GetFloat();
            if (GetToken() != ",")
                throw new Exception("expecting '{': tok = " + toks[0]);
            v.y = GetFloat();
            if (GetToken() != ",")
                throw new Exception("expecting '{': tok = " + toks[0]);
            v.z = GetFloat();
            if (GetToken() != "}")
                throw new Exception("expecting '{': tok = " + toks[0]);
            return v;
        }

        private void ReadSpatialProps(Entity e)
        {
            e.has_sp = true;
            if (GetToken() != "{")
                throw new Exception("expecting '{': tok = " + toks[0]);

            //NextToken();
            while (!IsToken("}"))
            {
                if (IsToken("orient"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    if (GetToken() != "{")
                        throw new Exception("expecting '{': tok = " + toks[0]);
                    e.rot1 = ReadVectorBlock();
                    if (GetToken() != ",")
                        throw new Exception("expecting ',': tok = " + toks[0]);
                    e.rot2 = ReadVectorBlock();
                    if (GetToken() != ",")
                        throw new Exception("expecting ',': tok = " + toks[0]);
                    e.rot3 = ReadVectorBlock();
                    if (GetToken() != "}")
                        throw new Exception("expecting '}': tok = " + toks[0]);
                }
                else if (IsToken("pos"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.pos = ReadVectorBlock();
                }
                else
                {
                    throw new Exception("expecting 'orient,pos': tok = " + toks[0]);
                }

                if (IsToken(","))
                    NextToken();
            }
            NextToken();
        }

        private void ReadLightProps(Entity e)
        {
            e.has_lp = true;
            NextToken();
            while (!IsToken("}"))
            {
                if (IsToken("ambient"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.lp_ambient = ReadVectorBlock();
                }
                else if (IsToken("atten"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.lp_atten = ReadVectorBlock();
                }
                else if (IsToken("direction"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.lp_direction = ReadVectorBlock();
                }
                else if (IsToken("color"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.lp_color = ReadVectorBlock();
                }
                else if (IsToken("specular"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.lp_specular = ReadVectorBlock();
                }
                else if (IsToken("diffuse"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.lp_diffuse = ReadVectorBlock();
                }
                else if (IsToken("theta"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.lp_theta = GetNumber();
                }
                else if (IsToken("type"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.lp_type = GetNumberConstant();
                }
                else if (IsToken("cutoff"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.lp_cutoff = GetFloat();
                }
                else if (IsToken("on"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.lp_on = GetNumberConstant();
                }
                else if (IsToken("range"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.lp_range = GetNumber();
                }
                else
                {
                    throw new Exception("unrecognised token: tok = " + toks[0]);
                }
                if (IsToken(","))
                    NextToken();
            }
            NextToken();
        }

        private void ReadUserProps(Entity e)
        {
            e.has_up = true;
            NextToken();
            while (!IsToken("}"))
            {
                if (IsToken("NoFog"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.up_nofog = GetString();
                }
                else if (IsToken("Priority"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.up_priority = GetString();
                }
                else if (IsToken("category"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.up_category = GetString();
                }
                else
                {
                    throw new Exception("unrecognised token: tok = " + toks[0]);
                }
                if (IsToken(","))
                    NextToken();
            }
            NextToken();
        }

        private void ReadPathProps(Entity e)
        {
            e.has_pp = true;
            NextToken();
            while (!IsToken("}"))
            {
                if (IsToken("path_data"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.pp_path_data = GetString();
                }
                else if (IsToken("path_type"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.pp_path_type = GetString();
                }
                else
                {
                    throw new Exception("unrecognised token: tok = " + toks[0]);
                }
                if (IsToken(","))
                    NextToken();
            }
            NextToken();
        }

        private void ReadAudioProps(Entity e)
        {
            e.has_ap = true;
            NextToken();
            while (!IsToken("}"))
            {
                if (IsToken("dmax"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.ap_dmax = GetNumber();
                }
                else if (IsToken("rmix"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.ap_rmix = GetNumber();
                }
                else if (IsToken("dmin"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.ap_dmin = GetNumber();
                }
                else if (IsToken("attenuation"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.ap_attenuation = GetNumber();
                }
                else if (IsToken("atout"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.ap_atout = GetNumber();
                }
                else if (IsToken("pan"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.ap_pan = GetNumber();
                }
                else if (IsToken("aout"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.ap_aout = GetNumber();
                }
                else if (IsToken("ain"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.ap_ain = GetNumber();
                }
                else
                {
                    throw new Exception("unrecognised token: tok = " + toks[0]);
                }
                if (IsToken(","))
                    NextToken();
            }
            NextToken();
        }

        private void ReadCameraProps(Entity e)
        {
            e.has_cp = true;
            NextToken();
            while (!IsToken("}"))
            {
                if (IsToken("farplane"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.cp_farplane = GetFloat();
                }
                else if (IsToken("nearplane"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.cp_nearplane = GetFloat();
                }
                else if (IsToken("hvaspect"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.cp_hvaspect = GetFloat();
                }
                else if (IsToken("fovh"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.cp_fovh = GetFloat();
                }
                else
                {
                    throw new Exception("unrecognised token: tok = " + toks[0]);
                }
                if (IsToken(","))
                    NextToken();
            }
            NextToken();
        }

        private void ReadEntityBlock(Entity e)
        {
            while (!IsToken("}"))
            {
                if (IsToken("spatialprops"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    ReadSpatialProps(e);
                }
                else if (IsToken("lightprops"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    ReadLightProps(e);
                }
                else if (IsToken("entity_name"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.entity_name = GetString();
                }
                else if (IsToken("template_name"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.template_name = GetString();
                }
                else if (IsToken("usr_flg"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.usr_flg = GetNumber();
                }
                else if (IsToken("type"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.type = GetNumberConstant();
                }
                else if (IsToken("srt_grp"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.srt_grp = GetNumber();
                }
                else if (IsToken("lt_grp"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.lt_grp = GetNumber();
                }
                else if (IsToken("front"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.front = GetNumberConstant();
                }
                else if (IsToken("up"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.up = GetNumberConstant();
                }
                else if (IsToken("ambient"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.ambient = ReadVectorBlock();
                }
                else if (IsToken("flags"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.flags = GetNumberConstant();
                }
                else if (IsToken("psysprops"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    if (GetToken() != "{")
                        throw new Exception("expecting '{': tok = " + toks[0]);
                    if (GetToken() != "sparam")
                        throw new Exception("expecting 'sparam': tok = " + toks[0]);
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    e.sparam = GetNumber();
                    if (GetToken() != "}")
                        throw new Exception("expecting '}': tok = " + toks[0]);
                }
                else if (IsToken("userprops"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    ReadUserProps(e);
                }
                else if (IsToken("pathprops"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    ReadPathProps(e);
                }
                else if (IsToken("audioprops"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    ReadAudioProps(e);
                }
                else if (IsToken("cameraprops"))
                {
                    NextToken();
                    if (GetToken() != "=")
                        throw new Exception("expecting '=': tok = " + toks[0]);
                    ReadCameraProps(e);
                }
                else
                {
                    //NextToken();
                    throw new Exception("unrecognised token: tok = " + toks[0]);
                }
                if (IsToken(","))
                    NextToken();
            }
            NextToken();
        }

        private void ReadEntitiesSection()
        {
            while (!IsToken("}"))
            {
                if (IsToken("{"))
                {
                    NextToken();
                    var e = new Entity();
                    ReadEntityBlock(e);
                    entities.Add(e);
                }
                if (IsToken(","))
                    NextToken();
            }
        }


        protected string GetToken()
        {
            string v = toks[0];
            toks.RemoveAt(0);
            return v;
        }

        protected string GetNumberConstant()
        {
            string v = toks[0];
            toks.RemoveAt(0);
            return v;
        }

        protected string GetString()
        {
            Match m = Regex.Match(toks[0], "^\"([^\"]*)\"");
            string v = m.Groups[1].Captures[0].Value;
            toks.RemoveAt(0);
            return v;
        }

        protected int GetNumber()
        {
            int v = Convert.ToInt32(toks[0]);
            toks.RemoveAt(0);
            return v;
        }

        protected float GetFloat()
        {
            float v = Convert.ToSingle(toks[0],CultureInfo.InvariantCulture);
            toks.RemoveAt(0);
            return v;
        }

        protected void NextToken()
        {
            toks.RemoveAt(0);
        }

        protected bool IsToken(string s)
        {
            return toks[0].ToLowerInvariant() == s.ToLowerInvariant();
        }

        public class Entity
        {
            public Vector ambient;
            public int ap_ain;
            public int ap_aout;
            public int ap_atout;
            public int ap_attenuation;
            public int ap_dmax;
            public int ap_dmin;
            public int ap_pan;
            public int ap_rmix;
            public float cp_farplane;
            public float cp_fovh;
            public float cp_hvaspect;
            public float cp_nearplane;
            public string entity_name;
            public string flags;
            public string front;
            public bool has_ap = false;
            public bool has_cp = false;

            // psysprops

            // light props
            public bool has_lp = false;
            public bool has_pp = false;
            public bool has_sp = false;
            public bool has_up = false;
            public bool hs_psp = false;
            public Vector lp_ambient;
            public Vector lp_atten;
            public Vector lp_color;
            public float lp_cutoff;
            public Vector lp_diffuse;
            public Vector lp_direction;
            public string lp_on;
            public int lp_range;
            public Vector lp_specular;
            public int lp_theta;
            public string lp_type;
            public int lt_grp;
            public Vector pos;

            // user props
            public string pp_path_data;
            public string pp_path_type;
            public Vector rot1;
            public Vector rot2;
            public Vector rot3;
            public int sparam;
            public int srt_grp;
            public string template_name;
            public string type;
            public string up;
            public string up_category;
            public string up_nofog;
            public string up_priority;
            public int usr_flg;

            // audio props
        }

        public class Vector
        {
            public float x, y, z;
        };
    }
}