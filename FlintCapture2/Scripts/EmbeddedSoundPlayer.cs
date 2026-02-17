using FlintCapture2;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace FlintCapture2.Scripts
{
    /// <summary>
    /// Embedded Sound Player provides tools to play audio from resources that are embedded inside the application.
    /// </summary>
    /*
    public static class EmbeddedSoundPlayer
    {
        private static readonly Dictionary<string, CachedSound> _cache = new();
        private static readonly IWavePlayer _outputDevice;
        private static readonly MixingSampleProvider _mixer;

        // todo: make it so that a small blip of cached sound doesn't play on program close

        static EmbeddedSoundPlayer()
        {
            // Create shared output device (single mixer)
            _outputDevice = new WaveOutEvent()
            {
                DesiredLatency = 80
            };
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
            {
                ReadFully = true
            };
            _outputDevice.Init(_mixer);
            _outputDevice.Play();
        }

        /// <summary>
        /// Plays an embedded WAV sound instantly (cached after first load).
        /// </summary>
        /// <param name="name">Sound file name (without .wav)</param>
        /// <param name="volume">Volume 0.0–1.0 (default 1.0)</param>
        public static void PlaySound(string name, float volume = 1.0f)
        {
            string resourceName = $"{PROJCONSTANTS.AssemblyName}.assets.sounds.{name}.wav";

            if (!_cache.TryGetValue(resourceName, out var sound))
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using Stream? stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    MessageBox.Show($"Sound resource not found: {resourceName}", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                sound = new CachedSound(stream);
                _cache[resourceName] = sound;
            }

            var input = new CachedSoundSampleProvider(sound);
            input.Volume = Math.Clamp(volume, 0f, 1f);
            _mixer.AddMixerInput(input);
        }

        public static TimeSpan GetSoundDuration(string name)
        {
            string resourceName = $"{PROJCONSTANTS.AssemblyName}.assets.sounds.{name}.wav";

            if (!_cache.TryGetValue(resourceName, out var sound))
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using Stream? stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null)
                    throw new FileNotFoundException($"Sound resource not found: {resourceName}");

                sound = new CachedSound(stream);
                _cache[resourceName] = sound;
            }

            int totalSamples = sound.AudioData.Length;
            int sampleRate = sound.WaveFormat.SampleRate;
            int channels = sound.WaveFormat.Channels;

            double seconds = (double)totalSamples / (sampleRate * channels);
            return TimeSpan.FromSeconds(seconds);
        }


        private static void ShowAllEmbeddedResources()
        {
            string output = "";
            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (string r in asm.GetManifestResourceNames())
                output += $"{r}\n";
            MessageBox.Show(output);
        }

        public static void Dispose()
        {
            _outputDevice?.Dispose();
        }

        // Small helper classes for caching sounds efficiently
        private class CachedSound
        {
            public float[] AudioData { get; }
            public WaveFormat WaveFormat { get; }

            public CachedSound(Stream wavStream)
            {
                using var reader = new WaveFileReader(wavStream);
                var wholeFile = new List<float>((int)(reader.Length / 4));
                var readBuffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
                int samplesRead;
                var sampleProvider = reader.ToSampleProvider();
                while ((samplesRead = sampleProvider.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    wholeFile.AddRange(readBuffer.AsSpan(0, samplesRead).ToArray());
                }

                AudioData = wholeFile.ToArray();
                WaveFormat = reader.WaveFormat;
            }
        }

        private class CachedSoundSampleProvider : ISampleProvider
        {
            private readonly CachedSound _cachedSound;
            private long _position;
            public float Volume { get; set; } = 1f;

            public CachedSoundSampleProvider(CachedSound cachedSound)
            {
                _cachedSound = cachedSound;
            }

            public int Read(float[] buffer, int offset, int count)
            {
                var availableSamples = _cachedSound.AudioData.Length - _position;
                var samplesToCopy = Math.Min(availableSamples, count);
                if (samplesToCopy <= 0) return 0;

                for (int i = 0; i < samplesToCopy; i++)
                    buffer[offset + i] = _cachedSound.AudioData[_position + i] * Volume;

                _position += samplesToCopy;
                return (int)samplesToCopy;
            }

            public WaveFormat WaveFormat => _cachedSound.WaveFormat;
        }
    }
    */

    /// <summary>
    /// Sound player for WPF developed by Kos | v5.4.1
    /// </summary>
    public static class EmbeddedSoundPlayer
    {
        private static readonly Dictionary<string, CachedSound> _cache = new();
        private static readonly IWavePlayer _outputDevice;
        private static readonly MixingSampleProvider _mixer;

        static EmbeddedSoundPlayer()
        {
            _outputDevice = new WaveOutEvent { DesiredLatency = 80 };
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)) { ReadFully = true };
            _outputDevice.Init(_mixer);
            _outputDevice.Play();
        }

        /// <summary>Play a one shot sound</summary>
        public static void PlaySound(string name, float volume = 1f)
        {
            var sound = GetCachedSound(name);
            var provider = new CachedSoundSampleProvider(sound) { Volume = Math.Clamp(volume, 0f, 1f) };
            _mixer.AddMixerInput(provider);
        }

        /// <summary>Play a tracked sound that can be paused, resumed, and has pitch/volume control</summary>
        public static SoundInstance PlayTracked(string name, float volume = 1f, float pitch = 1f)
        {
            var sound = GetCachedSound(name);
            return new SoundInstance(sound, _mixer, volume, pitch);
        }

        public static TimeSpan GetSoundDuration(string name)
        {
            var sound = GetCachedSound(name);
            return TimeSpan.FromSeconds((double)sound.AudioData.Length / (sound.WaveFormat.SampleRate * sound.WaveFormat.Channels));
        }

        public static void Dispose()
        {
            _outputDevice?.Dispose();
        }

        private static CachedSound GetCachedSound(string name)
        {
            string resourceName = $"{PROJCONSTANTS.AssemblyName}.assets.sounds.{name}.wav";
            if (_cache.TryGetValue(resourceName, out var sound)) return sound;

            Assembly asm = Assembly.GetExecutingAssembly();
            using Stream? stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                MessageBox.Show($"Sound resource not found:\n{resourceName}", "Audio Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new FileNotFoundException(resourceName);
            }

            sound = new CachedSound(stream);
            _cache[resourceName] = sound;
            return sound;
        }
    }

    /// <summary>Represents a WAV loaded in memory for fast playback</summary>
    public class CachedSound
    {
        public float[] AudioData { get; }
        public WaveFormat WaveFormat { get; }

        public CachedSound(Stream wavStream)
        {
            using var reader = new WaveFileReader(wavStream);
            var sampleProvider = reader.ToSampleProvider();

            var buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
            var samples = new List<float>();

            int read;
            while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
                samples.AddRange(buffer.AsSpan(0, read).ToArray());

            AudioData = samples.ToArray();
            WaveFormat = reader.WaveFormat;
        }
    }

    /// <summary>Represents a currently playing/tracked sound</summary>
    public class SoundInstance : ISampleProvider
    {
        private readonly CachedSound _sound;
        private readonly CachedSoundSampleProvider _source;
        private readonly VolumeSampleProvider _volumeProvider;
        private readonly ISampleProvider _finalProvider;
        private bool _paused;

        public event Action? PlaybackEnded;

        public SoundInstance(CachedSound sound, MixingSampleProvider mixer, float volume = 1f, float pitch = 1f)
        {
            _sound = sound;
            _source = new CachedSoundSampleProvider(sound);

            ISampleProvider chain = _source;
            _volumeProvider = new VolumeSampleProvider(chain) { Volume = Math.Clamp(volume, 0f, 1f) };
            chain = _volumeProvider;

            if (Math.Abs(pitch - 1f) > 0.001f)
            {
                chain = new SmbPitchShiftingSampleProvider(chain) { PitchFactor = pitch };
            }

            _finalProvider = new EndAwareSampleProvider(chain, () => PlaybackEnded?.Invoke());
            mixer.AddMixerInput(new PausableSampleProvider(this));
        }

        /// <summary>
        /// Total duration of this SoundInstance's audio source as a TimeSpan.
        /// </summary>
        public TimeSpan Duration => TimeSpan.FromSeconds((double)_sound.AudioData.Length / (_sound.WaveFormat.SampleRate * _sound.WaveFormat.Channels));
        /// <summary>
        /// Current position of this SoundInstance's playhead on the audio source.
        /// </summary>
        public TimeSpan Position => TimeSpan.FromSeconds((double)_source.Position / (_sound.WaveFormat.SampleRate * _sound.WaveFormat.Channels));
        public float Volume { get => _volumeProvider.Volume; set => _volumeProvider.Volume = Math.Clamp(value, 0f, 1f); }

        public void Pause() => _paused = true;
        public void Resume() => _paused = false;
        public void Stop() => _source.SeekToEnd();

        public int Read(float[] buffer, int offset, int count) => _paused ? FillSilent(buffer, offset, count) : _finalProvider.Read(buffer, offset, count);

        private static int FillSilent(float[] buffer, int offset, int count)
        {
            Array.Clear(buffer, offset, count);
            return count;
        }

        public WaveFormat WaveFormat => _sound.WaveFormat;

        private class PausableSampleProvider : ISampleProvider
        {
            private readonly SoundInstance _instance;
            public PausableSampleProvider(SoundInstance instance) => _instance = instance;
            public int Read(float[] buffer, int offset, int count) => _instance.Read(buffer, offset, count);
            public WaveFormat WaveFormat => _instance.WaveFormat;
        }

        private class EndAwareSampleProvider : ISampleProvider
        {
            private readonly ISampleProvider _source;
            private readonly Action _onEnd;
            private bool _ended;
            public EndAwareSampleProvider(ISampleProvider source, Action onEnd)
            {
                _source = source;
                _onEnd = onEnd;
            }
            public int Read(float[] buffer, int offset, int count)
            {
                int read = _source.Read(buffer, offset, count);
                if (read == 0 && !_ended) { _ended = true; _onEnd(); }
                return read;
            }
            public WaveFormat WaveFormat => _source.WaveFormat;
        }
    }

    /// <summary>Simple sample provider that plays a CachedSound</summary>
    public class CachedSoundSampleProvider : ISampleProvider
    {
        private readonly CachedSound _sound;
        public long Position { get; private set; }
        public float Volume { get; set; } = 1f;
        public CachedSoundSampleProvider(CachedSound sound) => _sound = sound;
        public int Read(float[] buffer, int offset, int count)
        {
            long available = _sound.AudioData.Length - Position;
            long toCopy = Math.Min(available, count);
            if (toCopy <= 0) return 0;
            for (int i = 0; i < toCopy; i++) buffer[offset + i] = _sound.AudioData[Position + i] * Volume;
            Position += toCopy;
            return (int)toCopy;
        }
        public void SeekToEnd() => Position = _sound.AudioData.Length;
        public WaveFormat WaveFormat => _sound.WaveFormat;
    }

    /// <summary>
    /// Embedded Sound Player v1.0 - Offers controls to play sound resources that are embedded inside the application.
    /// </summary>
    public static class LegacyEmbeddedSoundPlayer
    {
        private static readonly Dictionary<string, CachedSound> _cache = new();
        private static readonly IWavePlayer _outputDevice;
        private static readonly MixingSampleProvider _mixer;

        static LegacyEmbeddedSoundPlayer()
        {
            // Create shared output device (single mixer)
            _outputDevice = new WaveOutEvent()
            {
                DesiredLatency = 80
            };
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
            {
                ReadFully = true
            };
            _outputDevice.Init(_mixer);
            _outputDevice.Play();
        }

        /// <summary>
        /// Plays an embedded WAV sound instantly (cached after first load).
        /// </summary>
        /// <param name="name">Sound file name (without .wav)</param>
        /// <param name="volume">Volume 0.0–1.0 (default 1.0)</param>
        public static void PlaySound(string name, float volume = 1.0f)
        {
            string resourceName = $"{PROJCONSTANTS.AssemblyName}.assets.sounds.{name}.wav";

            if (!_cache.TryGetValue(resourceName, out var sound))
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using Stream? stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    MessageBox.Show($"Sound resource not found: {resourceName}", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                sound = new CachedSound(stream);
                _cache[resourceName] = sound;
            }

            var input = new CachedSoundSampleProvider(sound);
            input.Volume = Math.Clamp(volume, 0f, 1f);
            _mixer.AddMixerInput(input);
        }

        public static TimeSpan GetSoundDuration(string name)
        {
            string resourceName = $"{PROJCONSTANTS.AssemblyName}.assets.sounds.{name}.wav";

            if (!_cache.TryGetValue(resourceName, out var sound))
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using Stream? stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null)
                    throw new FileNotFoundException($"Sound resource not found: {resourceName}");

                sound = new CachedSound(stream);
                _cache[resourceName] = sound;
            }

            int totalSamples = sound.AudioData.Length;
            int sampleRate = sound.WaveFormat.SampleRate;
            int channels = sound.WaveFormat.Channels;

            double seconds = (double)totalSamples / (sampleRate * channels);
            return TimeSpan.FromSeconds(seconds);
        }


        private static void ShowAllEmbeddedResources()
        {
            string output = "";
            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (string r in asm.GetManifestResourceNames())
                output += $"{r}\n";
            MessageBox.Show(output);
        }

        public static void Dispose()
        {
            _outputDevice?.Dispose();
        }

        // Small helper classes for caching sounds efficiently
        private class CachedSound
        {
            public float[] AudioData { get; }
            public WaveFormat WaveFormat { get; }

            public CachedSound(Stream wavStream)
            {
                using var reader = new WaveFileReader(wavStream);
                var wholeFile = new List<float>((int)(reader.Length / 4));
                var readBuffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
                int samplesRead;
                var sampleProvider = reader.ToSampleProvider();
                while ((samplesRead = sampleProvider.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    wholeFile.AddRange(readBuffer.AsSpan(0, samplesRead).ToArray());
                }

                AudioData = wholeFile.ToArray();
                WaveFormat = reader.WaveFormat;
            }
        }

        private class CachedSoundSampleProvider : ISampleProvider
        {
            private readonly CachedSound _cachedSound;
            private long _position;
            public float Volume { get; set; } = 1f;

            public CachedSoundSampleProvider(CachedSound cachedSound)
            {
                _cachedSound = cachedSound;
            }

            public int Read(float[] buffer, int offset, int count)
            {
                var availableSamples = _cachedSound.AudioData.Length - _position;
                var samplesToCopy = Math.Min(availableSamples, count);
                if (samplesToCopy <= 0) return 0;

                for (int i = 0; i < samplesToCopy; i++)
                    buffer[offset + i] = _cachedSound.AudioData[_position + i] * Volume;

                _position += samplesToCopy;
                return (int)samplesToCopy;
            }

            public WaveFormat WaveFormat => _cachedSound.WaveFormat;
        }
    }
}
