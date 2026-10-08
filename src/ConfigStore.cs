using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace WinDeck
{
    /// <summary>설정 파일(%APPDATA%\WinDeck\config.json) 읽기·쓰기와 백업.</summary>
    public static class ConfigStore
    {
        static string dir;

        public static string Dir
        {
            get
            {
                if (dir == null)
                    dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinDeck");
                return dir;
            }
            set { dir = value; }
        }

        public static string ConfigPath
        {
            get { return Path.Combine(Dir, "config.json"); }
        }

        /// <summary>이번 실행에서 설정 파일을 새로 만들었는지 (첫 실행 안내용).</summary>
        public static bool IsNew { get; private set; }

        /// <summary>설정 파일이 손상되어 기본값으로 시작했을 때의 안내 문구.</summary>
        public static string LoadWarning { get; private set; }

        static JavaScriptSerializer Serializer()
        {
            return new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 64 };
        }

        public static AppConfig Load()
        {
            IsNew = false;
            LoadWarning = null;
            if (File.Exists(ConfigPath))
            {
                try
                {
                    return Parse(File.ReadAllText(ConfigPath, Encoding.UTF8));
                }
                catch (Exception ex)
                {
                    string broken = ConfigPath + ".broken-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    try { File.Copy(ConfigPath, broken, true); }
                    catch (Exception) { }
                    LoadWarning = "설정 파일을 읽지 못해 기본 설정으로 시작합니다.\n기존 파일은 다음 위치에 보관했습니다:\n"
                        + broken + "\n\n(" + ex.Message + ")";
                }
            }
            else
            {
                IsNew = true;
            }

            var c = AppConfig.CreateDefault();
            try { Save(c); }
            catch (Exception) { }
            return c;
        }

        public static AppConfig Parse(string json)
        {
            AppConfig c;
            try
            {
                c = Serializer().Deserialize<AppConfig>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("올바른 WinDeck 설정 파일이 아닙니다. (" + ex.Message + ")");
            }
            if (c == null || c.Pages == null)
                throw new InvalidDataException("올바른 WinDeck 설정 파일이 아닙니다.");
            c.Normalize();
            return c;
        }

        public static string ToJson(AppConfig c)
        {
            return PrettyJson(Serializer().Serialize(c));
        }

        public static AppConfig Clone(AppConfig c)
        {
            return Parse(ToJson(c));
        }

        public static void Save(AppConfig c)
        {
            WriteAtomic(ConfigPath, ToJson(c));
        }

        public static void Export(AppConfig c, string path)
        {
            File.WriteAllText(path, ToJson(c), new UTF8Encoding(false));
        }

        public static AppConfig Import(string path)
        {
            return Parse(File.ReadAllText(path, Encoding.UTF8));
        }

        /// <summary>현재 설정을 backups 폴더에 자동 보관하고 그 경로를 돌려준다.</summary>
        public static string AutoBackup(AppConfig c, string reason)
        {
            string backupDir = Path.Combine(Dir, "backups");
            Directory.CreateDirectory(backupDir);
            string path = Path.Combine(backupDir, reason + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json");
            File.WriteAllText(path, ToJson(c), new UTF8Encoding(false));
            return path;
        }

        static void WriteAtomic(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, content, new UTF8Encoding(false));
            if (!File.Exists(path))
            {
                File.Move(tmp, path);
                return;
            }
            try
            {
                File.Replace(tmp, path, null);
            }
            catch (Exception)
            {
                File.Copy(tmp, path, true);
                File.Delete(tmp);
            }
        }

        /// <summary>JavaScriptSerializer 출력은 한 줄이라 사람이 읽을 수 있게 들여쓰기한다.</summary>
        public static string PrettyJson(string json)
        {
            var sb = new StringBuilder(json.Length * 2);
            int indent = 0;
            bool inString = false;
            bool escape = false;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (inString)
                {
                    sb.Append(c);
                    if (escape) escape = false;
                    else if (c == '\\') escape = true;
                    else if (c == '"') inString = false;
                    continue;
                }
                switch (c)
                {
                    case '"':
                        inString = true;
                        sb.Append(c);
                        break;
                    case '{':
                    case '[':
                        sb.Append(c);
                        int j = i + 1;
                        while (j < json.Length && char.IsWhiteSpace(json[j])) j++;
                        if (j < json.Length && (json[j] == '}' || json[j] == ']'))
                        {
                            sb.Append(json[j]);
                            i = j;
                            break;
                        }
                        indent++;
                        NewLine(sb, indent);
                        break;
                    case '}':
                    case ']':
                        indent--;
                        NewLine(sb, indent);
                        sb.Append(c);
                        break;
                    case ',':
                        sb.Append(c);
                        NewLine(sb, indent);
                        break;
                    case ':':
                        sb.Append(": ");
                        break;
                    default:
                        if (!char.IsWhiteSpace(c)) sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        static void NewLine(StringBuilder sb, int indent)
        {
            sb.Append("\r\n");
            sb.Append(' ', Math.Max(0, indent) * 2);
        }
    }
}
