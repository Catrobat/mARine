using System;
using UnityEngine;

namespace CreateEnv.Ecosystem.Memory
{
    // Getting the report out of the app.
    //
    // On Android the file goes into the phone's Downloads folder through MediaStore
    // and is then offered to the share sheet. That route was chosen because it needs
    // no FileProvider entry, and therefore no change to AndroidManifest.xml — a file
    // shared by every scene in the project, including the ones this feature is not
    // allowed to touch. The project's minimum SDK is 29, which is exactly where
    // MediaStore's Downloads collection became available, so nothing is given up.
    //
    // It also means the PDF survives the app being uninstalled, which is the right
    // behaviour for something a learner is meant to hand in.
    //
    // In the Editor there is no share sheet, so the file is written and revealed in
    // Explorer instead. Same PDF, same code path up to the last step.
    public static class ReportShare
    {
        public struct Result
        {
            public bool ok;
            public string path;        // where it ended up
            public string failure;     // null when ok

            // What the toast says. Built here rather than in the interface because
            // only this code knows which route the file actually took — Downloads via
            // the share sheet, or a folder on a desktop.
            public string message;
        }

        // The one call the interface makes: build the report from a running reef and
        // hand it to the platform. Keeping the controller out of EcosystemReport is
        // what lets the report itself be built and checked without a scene.
        public static Result ShareReport(LivingReefController reef, string environmentName)
        {
            if (reef == null || reef.Sim == null)
                return Failed("There is no reef to report on yet.");

            byte[] pdf;
            try
            {
                pdf = EcosystemReport.Build(new EcosystemReport.Source
                {
                    environmentName = environmentName,
                    sim = reef.Sim,
                    health = reef.Health,
                    settings = reef.Settings,
                    octopuses = reef.Octopuses,
                    chronicle = reef.Chronicle,
                    reasons = reef.Reasons != null ? reef.Reasons.Active : null,
                });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return Failed("The report could not be put together.");
            }

            return Share(pdf, EcosystemReport.FileNameFor(environmentName));
        }

        static Result Failed(string why) =>
            new Result { failure = why, message = "Could not save the report. " + why };

        public static Result Share(byte[] pdf, string fileName)
        {
            if (pdf == null || pdf.Length == 0)
                return Failed("The report came out empty.");

            if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                fileName += ".pdf";

#if UNITY_ANDROID && !UNITY_EDITOR
            return ShareOnAndroid(pdf, fileName);
#else
            return SaveLocally(pdf, fileName);
#endif
        }

        // ── Editor and desktop ───────────────────────────────────────────────

        static Result SaveLocally(byte[] pdf, string fileName)
        {
            try
            {
                string folder = System.IO.Path.Combine(Application.persistentDataPath, "reports");
                System.IO.Directory.CreateDirectory(folder);

                string path = System.IO.Path.Combine(folder, fileName);
                System.IO.File.WriteAllBytes(path, pdf);

#if UNITY_EDITOR
                UnityEditor.EditorUtility.RevealInFinder(path);
#endif
                Debug.Log("[ReefReport] Written to " + path);
                return new Result
                {
                    ok = true,
                    path = path,
                    message = "Saved as " + fileName + Environment.NewLine + "in " + folder,
                };
            }
            catch (Exception e)
            {
                return Failed(e.Message);
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static Result ShareOnAndroid(byte[] pdf, string fileName)
        {
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var resolver = activity.Call<AndroidJavaObject>("getContentResolver");

                using var environment = new AndroidJavaClass("android.os.Environment");
                string downloads = environment.GetStatic<string>("DIRECTORY_DOWNLOADS");

                using var values = new AndroidJavaObject("android.content.ContentValues");
                values.Call("put", "_display_name", fileName);
                values.Call("put", "mime_type", "application/pdf");
                values.Call("put", "relative_path", downloads);

                // Marked pending while the bytes are written, so no other app can read a
                // half-finished PDF out of Downloads.
                using (var pending = new AndroidJavaObject("java.lang.Integer", 1))
                    values.Call("put", "is_pending", pending);

                using var downloadsClass = new AndroidJavaClass("android.provider.MediaStore$Downloads");
                using var collection = downloadsClass.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI");

                var item = resolver.Call<AndroidJavaObject>("insert", collection, values);
                if (item == null)
                    return Failed("Android would not make room in Downloads.");

                using (var stream = resolver.Call<AndroidJavaObject>("openOutputStream", item))
                {
                    if (stream == null)
                        return Failed("Android would not open the file for writing.");

                    stream.Call("write", pdf);
                    stream.Call("flush");
                    stream.Call("close");
                }

                // Clearing the pending flag is what makes the file visible in Downloads.
                // It is done defensively: if the JNI call cannot resolve, the bytes are
                // already written, and losing visibility is better than losing the file.
                try
                {
                    using var done = new AndroidJavaObject("android.content.ContentValues");
                    using var cleared = new AndroidJavaObject("java.lang.Integer", 0);
                    done.Call("put", "is_pending", cleared);
                    resolver.Call<int>("update", item, done, null, null);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[ReefReport] Could not clear the pending flag: " + e.Message);
                }

                // The file is safely on the device at this point. The share sheet is a
                // bonus: if it fails, the learner still has their report.
                string where = downloads + "/" + fileName;
                try
                {
                    using var send = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND");
                    send.Call<AndroidJavaObject>("setType", "application/pdf");
                    send.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", item);
                    send.Call<AndroidJavaObject>("putExtra", "android.intent.extra.SUBJECT", "My reef report");
                    send.Call<AndroidJavaObject>("addFlags", 1);   // FLAG_GRANT_READ_URI_PERMISSION

                    using var intentClass = new AndroidJavaClass("android.content.Intent");
                    using var chooser = intentClass.CallStatic<AndroidJavaObject>(
                        "createChooser", send, "Share your reef report");

                    activity.Call("startActivity", chooser);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[ReefReport] Saved, but the share sheet did not open: " + e.Message);
                    return new Result
                    {
                        ok = true,
                        path = where,
                        message = "Saved to your Downloads folder as " + fileName,
                    };
                }

                item.Dispose();
                return new Result
                {
                    ok = true,
                    path = where,
                    message = "Saved to your Downloads folder as " + fileName,
                };
            }
            catch (Exception e)
            {
                return Failed(e.Message);
            }
        }
#endif
    }
}
