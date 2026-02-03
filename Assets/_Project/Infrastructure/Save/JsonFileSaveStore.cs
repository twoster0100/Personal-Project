using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MyGame.Application.Save;

namespace MyGame.Infrastructure.Save
{
    /// <summary>
    ///  로컬 JSON 1차 진실 (persistentDataPath)
    /// - key에 경로 구분자(/)가 포함되어도 중간 폴더를 자동 생성하도록 보강
    ///   예) key = "save_player/<userId>/progress_0.json"
    /// </summary>
    public sealed class JsonFileSaveStore : ISaveStore
    {
        private readonly string _baseDir;

        public JsonFileSaveStore(string subFolder = "Saves")
        {
            _baseDir = Path.Combine(UnityEngine.Application.persistentDataPath, subFolder);
        }

        public Task WriteAsync(string key, string contents, CancellationToken ct = default)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                string finalPath = Path.Combine(_baseDir, key);

                // ✅ 핵심: 중간 폴더까지 생성 (key에 /가 들어오는 케이스)
                string dir = Path.GetDirectoryName(finalPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                string tmpPath = finalPath + ".tmp";
                string bakPath = finalPath + ".bak";

                File.WriteAllText(tmpPath, contents, Encoding.UTF8);

                // 가능한 경우 Replace로 원자적 교체 시도
                try
                {
                    if (File.Exists(finalPath))
                        File.Replace(tmpPath, finalPath, bakPath, ignoreMetadataErrors: true);
                    else
                        File.Move(tmpPath, finalPath);
                }
                catch
                {
                    // Replace 미지원/실패 플랫폼 fallback
                    if (File.Exists(finalPath))
                        File.Delete(finalPath);

                    File.Move(tmpPath, finalPath);
                }

                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        public Task<SaveReadResult> ReadAsync(string key, CancellationToken ct = default)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                string path = Path.Combine(_baseDir, key);

                if (!File.Exists(path))
                    return Task.FromResult(new SaveReadResult(false, null));

                string text = File.ReadAllText(path, Encoding.UTF8);
                return Task.FromResult(new SaveReadResult(true, text));
            }
            catch (Exception e)
            {
                return Task.FromException<SaveReadResult>(e);
            }
        }

        public Task DeleteAsync(string key, CancellationToken ct = default)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                string path = Path.Combine(_baseDir, key);

                if (File.Exists(path))
                    File.Delete(path);

                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }
    }
}
