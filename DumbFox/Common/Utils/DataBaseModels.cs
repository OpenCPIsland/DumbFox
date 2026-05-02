using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Utils
{
    public class FriendDB
    {
        public long Id { get; set; } = 1;
        public string meID { get; set; } = "999999999999999";
        public string hisID { get; set; } = "Fake Friend";
        public string username { get; set; } = "TestPlayer";
        public string hisusername { get; set; } = "Mixbot";
        public bool istrusted { get; set; } = false;
        public bool isfriend { get; set; } = false;
        public bool isrequested { get; set; } = false;
        public bool isdeleted { get; set; } = false;
        public long timestamp { get; set; } = 0;
    }

    public class ProfilesDB
    {
        public string Swid { get; set; } = "999999999999999";
        public long RegisterTimestamp { get; set; } = 0L;
        public int LangId { get; set; } = 1;
        public string Lang { get; set; } = "en-US";
        public string LastScene { get; set; } = "DefaultIgloo";
        public string LastWorld { get; set; } = "Alpine";
        public string Instance { get; set; } = string.Empty;
        public int Colour { get; set; } = 0;
        public int Auntarctic { get; set; } = 0;
        public int Djcadence { get; set; } = 0;
        public int Rockhopper { get; set; } = 0;
        public int Rookie { get; set; } = 0;
        public int Coins { get; set; } = 0;
        public int Chocolite { get; set; } = 0;
        public int Coconut { get; set; } = 0;
        public int Coin { get; set; } = 0;
        public int Confettiflower { get; set; } = 0;
        public int Crystalcluster { get; set; } = 0;
        public int Crystalice { get; set; } = 0;
        public int Crystalsmall { get; set; } = 0;
        public int Crystalsnowflake { get; set; } = 0;
        public int Flourflower { get; set; } = 0;
        public int Pearl { get; set; } = 0;
        public int Rainbowdrop { get; set; } = 0;
        public int Seasponge { get; set; } = 0;
        public int Seaweed { get; set; } = 0;
        public int Shell { get; set; } = 0;
        public int Windchime { get; set; } = 0;
        public bool Member { get; set; } = true;
        public int SelectedTubeId { get; set; } = 0;
        public string Accessory { get; set; } = "0";
        public int AccessoryTintIndex { get; set; } = 0;
        public double AccessoryXOffeset { get; set; } = 0;
        public double AccessoryYOffeset { get; set; } = 0;
        public string Costume { get; set; } = "0";
        public int CostumeTintIndex { get; set; } = 0;
        public double CostumeXOffeset { get; set; } = 0.0d;
        public double CostumeYOffeset { get; set; } = 0.0d;
        public string Brow { get; set; } = "0";
        public int BrowTintIndex { get; set; } = 0;
        public double BrowXOffeset { get; set; } = 0.0d;
        public double BrowYOffeset { get; set; } = 0.0d;
        public string Eyes { get; set; } = "0";
        public int EyesTintIndex { get; set; } = 0;
        public double EyesXOffeset { get; set; } = 0.0d;
        public double EyesYOffeset { get; set; } = 0.0d;
        public string Hair { get; set; } = "0";
        public int HairTintIndex { get; set; } = 0;
        public double HairXOffeset { get; set; } = 0.0d;
        public double HairYOffeset { get; set; } = 0.0d;
        public string Nose { get; set; } = "0";
        public int NoseTintIndex { get; set; } = 0;
        public double NoseXOffeset { get; set; } = 0.0d;
        public double NoseYOffeset { get; set; } = 0.0d;
        public string Mouth { get; set; } = "0";
        public int MouthTintIndex { get; set; } = 0;
        public double MouthXOffeset { get; set; } = 0.0d;
        public double MouthYOffeset { get; set; } = 0.0d;
        public string Skin { get; set; } = "0";
        public int SkinTintIndex { get; set; } = 0;
        public double SkinXOffeset { get; set; } = 0.0d;
        public double SkinYOffeset { get; set; } = 0.0d;
        public string Hat { get; set; } = "0";
        public int HatTintIndex { get; set; } = 0;
        public double HatXOffeset { get; set; } = 0.0d;
        public double HatYOffeset { get; set; } = 0.0d;
    }
}
