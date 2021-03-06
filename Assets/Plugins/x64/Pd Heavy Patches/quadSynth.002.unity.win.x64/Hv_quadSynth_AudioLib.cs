/**
 * Copyright (c) 2018 Enzien Audio, Ltd.
 * 
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions, and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the phrase "powered by heavy",
 *    the heavy logo, and a hyperlink to https://enzienaudio.com, all in a visible
 *    form.
 * 
 *   2.1 If the Application is distributed in a store system (for example,
 *       the Apple "App Store" or "Google Play"), the phrase "powered by heavy"
 *       shall be included in the app description or the copyright text as well as
 *       the in the app itself. The heavy logo will shall be visible in the app
 *       itself as well.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions;
using AOT;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(Hv_quadSynth_AudioLib))]
public class Hv_quadSynth_Editor : Editor {

  [MenuItem("Heavy/quadSynth")]
  static void CreateHv_quadSynth() {
    GameObject target = Selection.activeGameObject;
    if (target != null) {
      target.AddComponent<Hv_quadSynth_AudioLib>();
    }
  }
  
  private Hv_quadSynth_AudioLib _dsp;

  private void OnEnable() {
    _dsp = target as Hv_quadSynth_AudioLib;
  }

  public override void OnInspectorGUI() {
    bool isEnabled = _dsp.IsInstantiated();
    if (!isEnabled) {
      EditorGUILayout.LabelField("Press Play!",  EditorStyles.centeredGreyMiniLabel);
    }
    GUILayout.EndVertical();

    // parameters
    GUI.enabled = true;
    GUILayout.BeginVertical();
    EditorGUILayout.Space();
    EditorGUI.indentLevel++;
    
    // freqCutoff
    GUILayout.BeginHorizontal();
    float freqCutoff = _dsp.GetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Freqcutoff);
    float newFreqcutoff = EditorGUILayout.Slider("freqCutoff", freqCutoff, 0.0f, 20000.0f);
    if (freqCutoff != newFreqcutoff) {
      _dsp.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Freqcutoff, newFreqcutoff);
    }
    GUILayout.EndHorizontal();
    
    // gain
    GUILayout.BeginHorizontal();
    float gain = _dsp.GetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Gain);
    float newGain = EditorGUILayout.Slider("gain", gain, 0.0f, 1.0f);
    if (gain != newGain) {
      _dsp.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Gain, newGain);
    }
    GUILayout.EndHorizontal();
    
    // oscNote
    GUILayout.BeginHorizontal();
    float oscNote = _dsp.GetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Oscnote);
    float newOscnote = EditorGUILayout.Slider("oscNote", oscNote, 0.0f, 127.0f);
    if (oscNote != newOscnote) {
      _dsp.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Oscnote, newOscnote);
    }
    GUILayout.EndHorizontal();
    
    // ringModFreq
    GUILayout.BeginHorizontal();
    float ringModFreq = _dsp.GetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Ringmodfreq);
    float newRingmodfreq = EditorGUILayout.Slider("ringModFreq", ringModFreq, 0.0f, 20000.0f);
    if (ringModFreq != newRingmodfreq) {
      _dsp.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Ringmodfreq, newRingmodfreq);
    }
    GUILayout.EndHorizontal();
    
    // ringModMultiplier
    GUILayout.BeginHorizontal();
    float ringModMultiplier = _dsp.GetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Ringmodmultiplier);
    float newRingmodmultiplier = EditorGUILayout.Slider("ringModMultiplier", ringModMultiplier, 0.0f, 1.0f);
    if (ringModMultiplier != newRingmodmultiplier) {
      _dsp.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Ringmodmultiplier, newRingmodmultiplier);
    }
    GUILayout.EndHorizontal();
    
    // sawMult
    GUILayout.BeginHorizontal();
    float sawMult = _dsp.GetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Sawmult);
    float newSawmult = EditorGUILayout.Slider("sawMult", sawMult, 0.0f, 1.0f);
    if (sawMult != newSawmult) {
      _dsp.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Sawmult, newSawmult);
    }
    GUILayout.EndHorizontal();
    
    // sawOffset
    GUILayout.BeginHorizontal();
    float sawOffset = _dsp.GetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Sawoffset);
    float newSawoffset = EditorGUILayout.Slider("sawOffset", sawOffset, -24.0f, 24.0f);
    if (sawOffset != newSawoffset) {
      _dsp.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Sawoffset, newSawoffset);
    }
    GUILayout.EndHorizontal();
    
    // sinMult
    GUILayout.BeginHorizontal();
    float sinMult = _dsp.GetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Sinmult);
    float newSinmult = EditorGUILayout.Slider("sinMult", sinMult, 0.0f, 1.0f);
    if (sinMult != newSinmult) {
      _dsp.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Sinmult, newSinmult);
    }
    GUILayout.EndHorizontal();
    
    // sinOffset
    GUILayout.BeginHorizontal();
    float sinOffset = _dsp.GetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Sinoffset);
    float newSinoffset = EditorGUILayout.Slider("sinOffset", sinOffset, -24.0f, 24.0f);
    if (sinOffset != newSinoffset) {
      _dsp.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Sinoffset, newSinoffset);
    }
    GUILayout.EndHorizontal();
    
    // sqrMult
    GUILayout.BeginHorizontal();
    float sqrMult = _dsp.GetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Sqrmult);
    float newSqrmult = EditorGUILayout.Slider("sqrMult", sqrMult, 0.0f, 1.0f);
    if (sqrMult != newSqrmult) {
      _dsp.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Sqrmult, newSqrmult);
    }
    GUILayout.EndHorizontal();
    
    // sqrOffset
    GUILayout.BeginHorizontal();
    float sqrOffset = _dsp.GetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Sqroffset);
    float newSqroffset = EditorGUILayout.Slider("sqrOffset", sqrOffset, -24.0f, 24.0f);
    if (sqrOffset != newSqroffset) {
      _dsp.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Sqroffset, newSqroffset);
    }
    GUILayout.EndHorizontal();
    
    // triMult
    GUILayout.BeginHorizontal();
    float triMult = _dsp.GetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Trimult);
    float newTrimult = EditorGUILayout.Slider("triMult", triMult, 0.0f, 1.0f);
    if (triMult != newTrimult) {
      _dsp.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Trimult, newTrimult);
    }
    GUILayout.EndHorizontal();
    
    // triOffset
    GUILayout.BeginHorizontal();
    float triOffset = _dsp.GetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Trioffset);
    float newTrioffset = EditorGUILayout.Slider("triOffset", triOffset, -24.0f, 24.0f);
    if (triOffset != newTrioffset) {
      _dsp.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Trioffset, newTrioffset);
    }
    GUILayout.EndHorizontal();
    EditorGUI.indentLevel--;
  }
}
#endif // UNITY_EDITOR

[RequireComponent (typeof (AudioSource))]
public class Hv_quadSynth_AudioLib : MonoBehaviour {
  
  // Parameters are used to send float messages into the patch context (thread-safe).
  // Example usage:
  /*
    void Start () {
        Hv_quadSynth_AudioLib script = GetComponent<Hv_quadSynth_AudioLib>();
        // Get and set a parameter
        float freqCutoff = script.GetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Freqcutoff);
        script.SetFloatParameter(Hv_quadSynth_AudioLib.Parameter.Freqcutoff, freqCutoff + 0.1f);
    }
  */
  public enum Parameter : uint {
    Freqcutoff = 0x211BCFFA,
    Gain = 0x811CC33F,
    Oscnote = 0x184AAFEB,
    Ringmodfreq = 0x49F231A5,
    Ringmodmultiplier = 0x4FB6380A,
    Sawmult = 0xB16EB819,
    Sawoffset = 0x1C7A6BB7,
    Sinmult = 0xF77BD744,
    Sinoffset = 0xEBD86BBE,
    Sqrmult = 0x938B08DB,
    Sqroffset = 0xFA62263,
    Trimult = 0x12835BED,
    Trioffset = 0x14B03FDA,
  }
  
  // Delegate method for receiving float messages from the patch context (thread-safe).
  // Example usage:
  /*
    void Start () {
        Hv_quadSynth_AudioLib script = GetComponent<Hv_quadSynth_AudioLib>();
        script.RegisterSendHook();
        script.FloatReceivedCallback += OnFloatMessage;
    }

    void OnFloatMessage(Hv_quadSynth_AudioLib.FloatMessage message) {
        Debug.Log(message.receiverName + ": " + message.value);
    }
  */
  public class FloatMessage {
    public string receiverName;
    public float value;

    public FloatMessage(string name, float x) {
      receiverName = name;
      value = x;
    }
  }
  public delegate void FloatMessageReceived(FloatMessage message);
  public FloatMessageReceived FloatReceivedCallback;
  public float freqCutoff = 20000.0f;
  public float gain = 0.0f;
  public float oscNote = 0.0f;
  public float ringModFreq = 0.0f;
  public float ringModMultiplier = 0.0f;
  public float sawMult = 0.0f;
  public float sawOffset = 0.0f;
  public float sinMult = 1.0f;
  public float sinOffset = 0.0f;
  public float sqrMult = 0.0f;
  public float sqrOffset = 0.0f;
  public float triMult = 0.0f;
  public float triOffset = 0.0f;

  // internal state
  private Hv_quadSynth_Context _context;

  public bool IsInstantiated() {
    return (_context != null);
  }

  public void RegisterSendHook() {
    _context.RegisterSendHook();
  }
  
  // see Hv_quadSynth_AudioLib.Parameter for definitions
  public float GetFloatParameter(Hv_quadSynth_AudioLib.Parameter param) {
    switch (param) {
      case Parameter.Freqcutoff: return freqCutoff;
      case Parameter.Gain: return gain;
      case Parameter.Oscnote: return oscNote;
      case Parameter.Ringmodfreq: return ringModFreq;
      case Parameter.Ringmodmultiplier: return ringModMultiplier;
      case Parameter.Sawmult: return sawMult;
      case Parameter.Sawoffset: return sawOffset;
      case Parameter.Sinmult: return sinMult;
      case Parameter.Sinoffset: return sinOffset;
      case Parameter.Sqrmult: return sqrMult;
      case Parameter.Sqroffset: return sqrOffset;
      case Parameter.Trimult: return triMult;
      case Parameter.Trioffset: return triOffset;
      default: return 0.0f;
    }
  }

  public void SetFloatParameter(Hv_quadSynth_AudioLib.Parameter param, float x) {
    switch (param) {
      case Parameter.Freqcutoff: {
        x = Mathf.Clamp(x, 0.0f, 20000.0f);
        freqCutoff = x;
        break;
      }
      case Parameter.Gain: {
        x = Mathf.Clamp(x, 0.0f, 1.0f);
        gain = x;
        break;
      }
      case Parameter.Oscnote: {
        x = Mathf.Clamp(x, 0.0f, 127.0f);
        oscNote = x;
        break;
      }
      case Parameter.Ringmodfreq: {
        x = Mathf.Clamp(x, 0.0f, 20000.0f);
        ringModFreq = x;
        break;
      }
      case Parameter.Ringmodmultiplier: {
        x = Mathf.Clamp(x, 0.0f, 1.0f);
        ringModMultiplier = x;
        break;
      }
      case Parameter.Sawmult: {
        x = Mathf.Clamp(x, 0.0f, 1.0f);
        sawMult = x;
        break;
      }
      case Parameter.Sawoffset: {
        x = Mathf.Clamp(x, -24.0f, 24.0f);
        sawOffset = x;
        break;
      }
      case Parameter.Sinmult: {
        x = Mathf.Clamp(x, 0.0f, 1.0f);
        sinMult = x;
        break;
      }
      case Parameter.Sinoffset: {
        x = Mathf.Clamp(x, -24.0f, 24.0f);
        sinOffset = x;
        break;
      }
      case Parameter.Sqrmult: {
        x = Mathf.Clamp(x, 0.0f, 1.0f);
        sqrMult = x;
        break;
      }
      case Parameter.Sqroffset: {
        x = Mathf.Clamp(x, -24.0f, 24.0f);
        sqrOffset = x;
        break;
      }
      case Parameter.Trimult: {
        x = Mathf.Clamp(x, 0.0f, 1.0f);
        triMult = x;
        break;
      }
      case Parameter.Trioffset: {
        x = Mathf.Clamp(x, -24.0f, 24.0f);
        triOffset = x;
        break;
      }
      default: return;
    }
    if (IsInstantiated()) _context.SendFloatToReceiver((uint) param, x);
  }
  
  public void FillTableWithMonoAudioClip(string tableName, AudioClip clip) {
    if (clip.channels > 1) {
      Debug.LogWarning("Hv_quadSynth_AudioLib: Only loading first channel of '" +
          clip.name + "' into table '" +
          tableName + "'. Multi-channel files are not supported.");
    }
    float[] buffer = new float[clip.samples]; // copy only the 1st channel
    clip.GetData(buffer, 0);
    _context.FillTableWithFloatBuffer(tableName, buffer);
  }

  public void FillTableWithFloatBuffer(string tableName, float[] buffer) {
    _context.FillTableWithFloatBuffer(tableName, buffer);
  }

  private void Awake() {
    _context = new Hv_quadSynth_Context((double) AudioSettings.outputSampleRate);
  }
  
  private void Start() {
    _context.SendFloatToReceiver((uint) Parameter.Freqcutoff, freqCutoff);
    _context.SendFloatToReceiver((uint) Parameter.Gain, gain);
    _context.SendFloatToReceiver((uint) Parameter.Oscnote, oscNote);
    _context.SendFloatToReceiver((uint) Parameter.Ringmodfreq, ringModFreq);
    _context.SendFloatToReceiver((uint) Parameter.Ringmodmultiplier, ringModMultiplier);
    _context.SendFloatToReceiver((uint) Parameter.Sawmult, sawMult);
    _context.SendFloatToReceiver((uint) Parameter.Sawoffset, sawOffset);
    _context.SendFloatToReceiver((uint) Parameter.Sinmult, sinMult);
    _context.SendFloatToReceiver((uint) Parameter.Sinoffset, sinOffset);
    _context.SendFloatToReceiver((uint) Parameter.Sqrmult, sqrMult);
    _context.SendFloatToReceiver((uint) Parameter.Sqroffset, sqrOffset);
    _context.SendFloatToReceiver((uint) Parameter.Trimult, triMult);
    _context.SendFloatToReceiver((uint) Parameter.Trioffset, triOffset);
  }
  
  private void Update() {
    // retreive sent messages
    if (_context.IsSendHookRegistered()) {
      Hv_quadSynth_AudioLib.FloatMessage tempMessage;
      while ((tempMessage = _context.msgQueue.GetNextMessage()) != null) {
        FloatReceivedCallback(tempMessage);
      }
    }
  }

  private void OnAudioFilterRead(float[] buffer, int numChannels) {
    Assert.AreEqual(numChannels, _context.GetNumOutputChannels()); // invalid channel configuration
    _context.Process(buffer, buffer.Length / numChannels); // process dsp
  }
}

class Hv_quadSynth_Context {

#if UNITY_IOS && !UNITY_EDITOR
  private const string _dllName = "__Internal";
#else
  private const string _dllName = "Hv_quadSynth_AudioLib";
#endif

  // Thread-safe message queue
  public class SendMessageQueue {
    private readonly object _msgQueueSync = new object();
    private readonly Queue<Hv_quadSynth_AudioLib.FloatMessage> _msgQueue = new Queue<Hv_quadSynth_AudioLib.FloatMessage>();

    public Hv_quadSynth_AudioLib.FloatMessage GetNextMessage() {
      lock (_msgQueueSync) {
        return (_msgQueue.Count != 0) ? _msgQueue.Dequeue() : null;
      }
    }

    public void AddMessage(string receiverName, float value) {
      Hv_quadSynth_AudioLib.FloatMessage msg = new Hv_quadSynth_AudioLib.FloatMessage(receiverName, value);
      lock (_msgQueueSync) {
        _msgQueue.Enqueue(msg);
      }
    }
  }

  public readonly SendMessageQueue msgQueue = new SendMessageQueue();
  private readonly GCHandle gch;
  private readonly IntPtr _context; // handle into unmanaged memory
  private SendHook _sendHook = null;

  [DllImport (_dllName)]
  private static extern IntPtr hv_quadSynth_new_with_options(double sampleRate, int poolKb, int inQueueKb, int outQueueKb);

  [DllImport (_dllName)]
  private static extern int hv_processInlineInterleaved(IntPtr ctx,
      [In] float[] inBuffer, [Out] float[] outBuffer, int numSamples);

  [DllImport (_dllName)]
  private static extern void hv_delete(IntPtr ctx);

  [DllImport (_dllName)]
  private static extern double hv_getSampleRate(IntPtr ctx);

  [DllImport (_dllName)]
  private static extern int hv_getNumInputChannels(IntPtr ctx);

  [DllImport (_dllName)]
  private static extern int hv_getNumOutputChannels(IntPtr ctx);

  [DllImport (_dllName)]
  private static extern void hv_setSendHook(IntPtr ctx, SendHook sendHook);

  [DllImport (_dllName)]
  private static extern void hv_setPrintHook(IntPtr ctx, PrintHook printHook);

  [DllImport (_dllName)]
  private static extern int hv_setUserData(IntPtr ctx, IntPtr userData);

  [DllImport (_dllName)]
  private static extern IntPtr hv_getUserData(IntPtr ctx);

  [DllImport (_dllName)]
  private static extern void hv_sendBangToReceiver(IntPtr ctx, uint receiverHash);

  [DllImport (_dllName)]
  private static extern void hv_sendFloatToReceiver(IntPtr ctx, uint receiverHash, float x);

  [DllImport (_dllName)]
  private static extern uint hv_msg_getTimestamp(IntPtr message);

  [DllImport (_dllName)]
  private static extern bool hv_msg_hasFormat(IntPtr message, string format);

  [DllImport (_dllName)]
  private static extern float hv_msg_getFloat(IntPtr message, int index);

  [DllImport (_dllName)]
  private static extern bool hv_table_setLength(IntPtr ctx, uint tableHash, uint newSampleLength);

  [DllImport (_dllName)]
  private static extern IntPtr hv_table_getBuffer(IntPtr ctx, uint tableHash);

  [DllImport (_dllName)]
  private static extern float hv_samplesToMilliseconds(IntPtr ctx, uint numSamples);

  [DllImport (_dllName)]
  private static extern uint hv_stringToHash(string s);

  private delegate void PrintHook(IntPtr context, string printName, string str, IntPtr message);

  private delegate void SendHook(IntPtr context, string sendName, uint sendHash, IntPtr message);

  public Hv_quadSynth_Context(double sampleRate, int poolKb=10, int inQueueKb=13, int outQueueKb=2) {
    gch = GCHandle.Alloc(msgQueue);
    _context = hv_quadSynth_new_with_options(sampleRate, poolKb, inQueueKb, outQueueKb);
    hv_setPrintHook(_context, new PrintHook(OnPrint));
    hv_setUserData(_context, GCHandle.ToIntPtr(gch));
  }

  ~Hv_quadSynth_Context() {
    hv_delete(_context);
    GC.KeepAlive(_context);
    GC.KeepAlive(_sendHook);
    gch.Free();
  }

  public void RegisterSendHook() {
    // Note: send hook functionality only applies to messages containing a single float value
    if (_sendHook == null) {
      _sendHook = new SendHook(OnMessageSent);
      hv_setSendHook(_context, _sendHook);
    }
  }

  public bool IsSendHookRegistered() {
    return (_sendHook != null);
  }

  public double GetSampleRate() {
    return hv_getSampleRate(_context);
  }

  public int GetNumInputChannels() {
    return hv_getNumInputChannels(_context);
  }

  public int GetNumOutputChannels() {
    return hv_getNumOutputChannels(_context);
  }

  public void SendBangToReceiver(uint receiverHash) {
    hv_sendBangToReceiver(_context, receiverHash);
  }

  public void SendFloatToReceiver(uint receiverHash, float x) {
    hv_sendFloatToReceiver(_context, receiverHash, x);
  }

  public void FillTableWithFloatBuffer(string tableName, float[] buffer) {
    uint tableHash = hv_stringToHash(tableName);
    if (hv_table_getBuffer(_context, tableHash) != IntPtr.Zero) {
      hv_table_setLength(_context, tableHash, (uint) buffer.Length);
      Marshal.Copy(buffer, 0, hv_table_getBuffer(_context, tableHash), buffer.Length);
    } else {
      Debug.Log(string.Format("Table '{0}' doesn't exist in the patch context.", tableName));
    }
  }

  public uint StringToHash(string s) {
    return hv_stringToHash(s);
  }

  public int Process(float[] buffer, int numFrames) {
    return hv_processInlineInterleaved(_context, buffer, buffer, numFrames);
  }

  [MonoPInvokeCallback(typeof(PrintHook))]
  private static void OnPrint(IntPtr context, string printName, string str, IntPtr message) {
    float timeInSecs = hv_samplesToMilliseconds(context, hv_msg_getTimestamp(message)) / 1000.0f;
    Debug.Log(string.Format("{0} [{1:0.000}]: {2}", printName, timeInSecs, str));
  }

  [MonoPInvokeCallback(typeof(SendHook))]
  private static void OnMessageSent(IntPtr context, string sendName, uint sendHash, IntPtr message) {
    if (hv_msg_hasFormat(message, "f")) {
      SendMessageQueue msgQueue = (SendMessageQueue) GCHandle.FromIntPtr(hv_getUserData(context)).Target;
      msgQueue.AddMessage(sendName, hv_msg_getFloat(message, 0));
    }
  }
}
