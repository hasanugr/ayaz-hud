using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;

namespace AyazHUD.Cekirdek
{
    /// <summary>
    /// "AyazHUD" zamanlanmış görevi. Görev uygulamayı en yüksek yetkiyle çalıştırır; böylece sensör sürücüsü
    /// ve PresentMon için gereken yönetici yetkisi her açılışta UAC sormadan alınır.
    /// Oturum açılışı tetikleyicisi "Windows ile başlat" ayarıdır; görev kendisi her zaman durur ki
    /// Başlat menüsünden açılış da UAC'siz olsun.
    /// </summary>
    internal static class Gorev
    {
        public const string Ad = "AyazHUD";

        const int TASK_TRIGGER_LOGON = 9;
        const int TASK_ACTION_EXEC = 0;
        const int TASK_CREATE_OR_UPDATE = 6;
        const int TASK_LOGON_INTERACTIVE_TOKEN = 3;
        const int TASK_RUNLEVEL_HIGHEST = 1;
        const int TASK_INSTANCES_IGNORE_NEW = 2;

        static dynamic Servis()
        {
            dynamic servis = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service"));
            servis.Connect();
            return servis;
        }

        static dynamic Bul()
        {
            try
            {
                return Servis().GetFolder("\\").GetTask(Ad);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool VarMi() => Bul() != null;

        /// <summary>Görevi çalıştırır (yönetici olmayan süreçten de olur). Görev yoksa false.</summary>
        public static bool Calistir()
        {
            try
            {
                dynamic gorev = Bul();
                if (gorev == null) return false;
                gorev.Run(null);
                return true;
            }
            catch (Exception e)
            {
                Gunluk.Yaz("Görev çalıştırılamadı", e);
                return false;
            }
        }

        /// <summary>Görevi geçerli kullanıcı için oluşturur ya da günceller. Yönetici yetkisi gerekir.</summary>
        public static void Kur(bool oturumAcilisinda)
        {
            string exe = Process.GetCurrentProcess().MainModule.FileName;
            string kullanici = WindowsIdentity.GetCurrent().User.Value;

            dynamic servis = Servis();
            dynamic tanim = servis.NewTask(0);
            tanim.RegistrationInfo.Author = "Ayaz HUD";
            tanim.RegistrationInfo.Description = "Ayaz HUD'u yönetici yetkisiyle, UAC sormadan başlatır (sıcaklık sensörleri ve FPS ölçümü için).";

            tanim.Principal.UserId = kullanici;
            tanim.Principal.LogonType = TASK_LOGON_INTERACTIVE_TOKEN;
            tanim.Principal.RunLevel = TASK_RUNLEVEL_HIGHEST;

            tanim.Settings.DisallowStartIfOnBatteries = false;
            tanim.Settings.StopIfGoingOnBatteries = false;
            tanim.Settings.ExecutionTimeLimit = "PT0S";
            tanim.Settings.MultipleInstances = TASK_INSTANCES_IGNORE_NEW;
            tanim.Settings.Priority = 6; // normal öncelik (varsayılan 7 düşük öncelik)
            tanim.Settings.StartWhenAvailable = false;

            dynamic tetik = tanim.Triggers.Create(TASK_TRIGGER_LOGON);
            tetik.UserId = kullanici;
            tetik.Delay = "PT5S";
            tetik.Enabled = oturumAcilisinda;

            dynamic eylem = tanim.Actions.Create(TASK_ACTION_EXEC);
            eylem.Path = exe;
            eylem.Arguments = "--gorev";
            eylem.WorkingDirectory = Path.GetDirectoryName(exe);

            servis.GetFolder("\\").RegisterTaskDefinition(Ad, tanim, TASK_CREATE_OR_UPDATE, null, null, TASK_LOGON_INTERACTIVE_TOKEN, null);
            Gunluk.Yaz($"Görev kuruldu (oturum açılışında: {oturumAcilisinda})");
        }

        /// <summary>Oturum açılışında başlama durumu; görev yoksa null.</summary>
        public static bool? OturumAcilisinda()
        {
            try
            {
                dynamic gorev = Bul();
                if (gorev == null) return null;
                dynamic tetikler = gorev.Definition.Triggers;
                for (int i = 1; i <= tetikler.Count; i++)
                {
                    dynamic t = tetikler.Item(i);
                    if (t.Type == TASK_TRIGGER_LOGON) return (bool)t.Enabled;
                }
                return false;
            }
            catch (Exception e)
            {
                Gunluk.Yaz("Görev okunamadı", e);
                return null;
            }
        }

        public static void Sil()
        {
            try
            {
                Servis().GetFolder("\\").DeleteTask(Ad, 0);
                Gunluk.Yaz("Görev silindi");
            }
            catch (Exception e)
            {
                Gunluk.Yaz("Görev silinemedi", e);
            }
        }
    }
}
