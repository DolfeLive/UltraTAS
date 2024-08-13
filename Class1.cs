using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BepInEx;
using Configgy;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using WindowsInput;
using WindowsInput.Native;

namespace _UltraTAS
{
	// Token: 0x02000003 RID: 3
	[BepInPlugin("UltraTAS", "UltraTAS", "1.0.0")]
	public class Class1 : BaseUnityPlugin
	{
		// Token: 0x06000007 RID: 7 RVA: 0x000020F4 File Offset: 0x000002F4
		private void Awake()
		{
			Random.InitState(0);
			Class1.RefreshTASList();
			this.Simulator = new InputSimulator();
			Class1.cfgB = new ConfigBuilder("UltraTAS", null);
			Class1.cfgB.BuildAll();
			base.StartCoroutine(this.CustUpdate());
			base.Logger.LogInfo("DolfeTAS Mod Loaded");
			new Harmony("DolfeMODS.Ultrakill.UltraTAS").PatchAll();
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002160 File Offset: 0x00000360
		private static void RefreshTASList()
		{
			Class1.FileSavePath = Paths.ConfigPath + "/UltraTAS/";
			if (!Directory.Exists(Class1.FileSavePath))
			{
				Directory.CreateDirectory(Class1.FileSavePath);
			}
			Class1.TASList = Directory.GetFiles(Class1.FileSavePath, "*DolfeTAS");
			if (Class1.TASList.Length != 0)
			{
				foreach (string path in Class1.TASList)
				{
					Class1.TempTAS.Add(Path.GetFileName(path));
				}
				if (Class1.cfgB != null)
				{
					Class1.TasReplayName.SetOptions(Class1.TASList, Class1.TempTAS.ToArray(), 0, 0);
				}
				if (Class1.cfgB == null)
				{
					Class1.TasReplayName = new ConfigDropdown<string>(Class1.TASList, Class1.TempTAS.ToArray(), 0);
				}
				if (Class1.cfgB != null)
				{
					Class1.cfgB.Rebuild();
					Class1.cfgB.BuildAll();
					return;
				}
			}
			else
			{
				List<string> list = new List<string>
				{
					"PLEASE RECORD A TAS FIRST"
				};
				Class1.TasReplayName = new ConfigDropdown<string>(list.ToArray(), list.ToArray(), 0);
			}
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002268 File Offset: 0x00000468
		private void Update()
		{
			if (!this.LoopRunning)
			{
				base.StartCoroutine(this.CustUpdate());
				base.Logger.LogInfo("Started Loop");
			}
			if (this.status == null)
			{
				GameObject gameObject = new GameObject("StatusText");
				gameObject.transform.SetParent(GameObject.Find("Canvas").transform, false);
				this.status = gameObject.AddComponent<Text>();
				this.status.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
				this.status.color = Color.white;
				this.status.fontSize = 20;
				this.status.alignment = 0;
				RectTransform component = this.status.GetComponent<RectTransform>();
				component.anchorMin = new Vector2(0f, 1f);
				component.anchorMax = new Vector2(0f, 1f);
				component.pivot = new Vector2(0f, 1f);
				component.anchoredPosition = new Vector2(0f, -1f);
				component.sizeDelta = new Vector2(2200f, 10000f);
				component.anchoredPosition += new Vector2(0f, 0f);
				CanvasGroup canvasGroup = gameObject.AddComponent<CanvasGroup>();
				canvasGroup.interactable = false;
				canvasGroup.blocksRaycasts = false;
			}
			if (this.status != null)
			{
				try
				{
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.Clear();
					foreach (VirtualKeyCode virtualKeyCode in this.pressedSpecialKeys)
					{
						stringBuilder.Append(string.Format("Key: {0} \n", virtualKeyCode));
					}
					foreach (Key key in this.pressedDownKeys)
					{
						stringBuilder.Append(string.Format("Key: {0} \n", key));
					}
					string text = stringBuilder.ToString();
					this.status.text = string.Concat(new string[]
					{
						"\r\nTime Paused: ",
						this.TimePaused.ToString(),
						"\r\nRecording TAS: ",
						this.CaptureInputs.ToString(),
						"\r\nCurrent TAS Recoding to: ",
						Class1.TasName.Value,
						"\r\nPlaying TAS: ",
						this.PlayingTAS.ToString(),
						"\r\nSelected TAS Name: ",
						Class1.TempTAS[Class1.TasReplayName.currentIndex.Value],
						"\r\nAll Active Keys (Being played by TAS): \r\n",
						text,
						"\r\n                "
					});
				}
				catch (Exception)
				{
				}
			}
			if (this.KeysStatus.Count == 0)
			{
				foreach (object obj in Enum.GetValues(typeof(Keys)))
				{
					Keys item = (Keys)obj;
					this.KeysStatus.Add(new ValueTuple<VirtualKeyCode, bool>(item, false));
				}
			}
			if (this.dic2.Count == 0 && MonoSingleton<InputManager>.Instance != null)
			{
				Dictionary<string, KeyCode> inputsDictionary = MonoSingleton<InputManager>.Instance.inputsDictionary;
				if (inputsDictionary != null)
				{
					Dictionary<KeyCode, string> dictionary = inputsDictionary.ToDictionary((KeyValuePair<string, KeyCode> x) => x.Value, (KeyValuePair<string, KeyCode> x) => x.Key);
					this.dic2 = new Dictionary<KeyCode, string>(dictionary);
					return;
				}
				Debug.LogError("inputsDictionary is null");
			}
		}

		// Token: 0x0600000A RID: 10 RVA: 0x0000267C File Offset: 0x0000087C
		private IEnumerator CustUpdate()
		{
			this.LoopRunning = true;
			if (Input.GetKeyDown(Class1.StartRecording.Value))
			{
				this.CaptureInputs = !this.CaptureInputs;
				MonoBehaviour.print(this.CaptureInputs.ToString());
				if (!this.CaptureInputs)
				{
					this.frame = 0;
					MonoBehaviour.print("RECORDING TAS ENDED");
					List<string> list = new List<string>();
					list.Add("END");
					File.AppendAllLines(this.CurrentTASFile, list);
				}
				if (this.CaptureInputs)
				{
					this.MakeFileAndStuff();
					MonoBehaviour.print("RECORDING TAS STARTED");
				}
			}
			if (Input.GetKeyDown(Class1.PlayTAS.Value))
			{
				this.StartTASReplay();
			}
			if (Input.GetKeyDown(Class1.PauseGame.Value))
			{
				this.TimePaused = !this.TimePaused;
				Debug.Log("Time Paused = " + this.TimePaused.ToString());
				if (this.TimePaused)
				{
					this.prevTimeScale = Time.timeScale;
				}
			}
			if (this.TimePaused)
			{
				Time.timeScale = 0f;
			}
			if (Time.timeScale == 0f && !this.TimePaused && !MonoSingleton<OptionsManager>.Instance.paused)
			{
				Time.timeScale = this.prevTimeScale;
			}
			if (Input.GetKeyDown(Class1.AdvFrame.Value))
			{
				base.StartCoroutine(this.AdvanceFrame());
			}
			if (this.actionMappings.Count == 0 && MonoSingleton<InputManager>.instance != null)
			{
				List<ValueTuple<string, KeyCode>> list2 = new List<ValueTuple<string, KeyCode>>();
				foreach (string text in this.PlayerActions)
				{
					ValueTuple<string, KeyCode?> valueTuple = new ValueTuple<string, KeyCode?>(text, this.GetKeyCodeFromInputsDic(text));
					if (valueTuple.Item2 != null)
					{
						list2.Add(new ValueTuple<string, KeyCode>(valueTuple.Item1, valueTuple.Item2.Value));
					}
				}
				Dictionary<string, VirtualKeyCode> dictionary = new Dictionary<string, VirtualKeyCode>();
				foreach (ValueTuple<string, KeyCode> valueTuple2 in list2)
				{
					VirtualKeyCode? virtualKeyCode = Class1.GetVirtualKeyCode(valueTuple2.Item2);
					if (virtualKeyCode != null)
					{
						dictionary.Add(valueTuple2.Item1, virtualKeyCode.Value);
					}
				}
				this.actionMappings = dictionary;
			}
			if (this.CaptureInputs)
			{
				List<string> list3 = new List<string>();
				if (MonoSingleton<InputManager>.Instance != null)
				{
					foreach (KeyValuePair<string, KeyCode> keyValuePair in MonoSingleton<InputManager>.Instance.inputsDictionary)
					{
						if (Input.GetKey(keyValuePair.Value))
						{
							list3.Add(this.GetStringFromInputsDic(keyValuePair.Value));
							MonoBehaviour.print("Pressing: Key: " + this.GetStringFromInputsDic(keyValuePair.Value));
						}
					}
				}
				if (!this.TimePaused || (this.TimePaused && Input.GetKeyDown(Class1.AdvFrame.Value)))
				{
					this.frame++;
					MonoBehaviour.print(string.Format("Frame: {0}", this.frame));
					list3.Add("DOLF" + this.frame.ToString());
					list3.Add("X" + MonoSingleton<CameraController>.instance.rotationX.ToString());
					list3.Add("Y" + MonoSingleton<CameraController>.instance.rotationY.ToString());
				}
				this.SaveFrameData(list3);
			}
			yield return new WaitForEndOfFrame();
			base.StartCoroutine(this.CustUpdate());
			yield break;
		}

		// Token: 0x0600000B RID: 11 RVA: 0x0000268B File Offset: 0x0000088B
		private void SaveFrameData(List<string> TASLogFile)
		{
			this.WriteToFile(TASLogFile);
			TASLogFile.Clear();
		}

		// Token: 0x0600000C RID: 12 RVA: 0x0000269A File Offset: 0x0000089A
		private void WriteToFile(List<string> text)
		{
			File.AppendAllLines(this.CurrentTASFile, text);
		}

		// Token: 0x0600000D RID: 13 RVA: 0x000026A8 File Offset: 0x000008A8
		private string makeNewSave(string TASName)
		{
			return Class1.FileSavePath + TASName;
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000026B8 File Offset: 0x000008B8
		private void MakeFileAndStuff()
		{
			Directory.CreateDirectory(Class1.FileSavePath);
			ConfigInputField<string> tasName = Class1.TasName;
			string text = this.makeNewSave(((tasName != null) ? tasName.ToString() : null) + ".DolfeTAS");
			using (File.Create(text))
			{
			}
			this.CurrentTASFile = text;
		}

		// Token: 0x0600000F RID: 15 RVA: 0x0000271C File Offset: 0x0000091C
		private void StartTASReplay()
		{
			if (this.PlayingTAS)
			{
				base.StartCoroutine(this.TASCheck());
				MonoBehaviour.print("Playing TAS");
				return;
			}
			this.PlayingTAS = true;
			base.StartCoroutine(this.ReplayTASDos());
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00002752 File Offset: 0x00000952
		private IEnumerator AdvanceFrame()
		{
			Time.timeScale = 1f;
			yield return new WaitForEndOfFrame();
			Time.timeScale = 0f;
			yield break;
		}

		// Token: 0x06000011 RID: 17 RVA: 0x0000275A File Offset: 0x0000095A
		private IEnumerator TASCheck()
		{
			bool keepRunning = true;
			yield return new WaitForSeconds(0.05f);
			while (keepRunning || this.PlayingTAS)
			{
				if (Input.GetKey(Class1.PlayTAS.Value))
				{
					this.ReplayINT = -1;
					keepRunning = false;
					this.PlayingTAS = false;
					MonoBehaviour.print("Stopping TAS Replay");
				}
				yield return new WaitForEndOfFrame();
			}
			yield break;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x00002769 File Offset: 0x00000969
		private IEnumerator ReplayTASDos()
		{
			MonoBehaviour.print("Playing TAS Started");
			Class1.wasTSUsedThisScene = true;
			string[] lines = File.ReadAllLines(Class1.TasReplayName.Value);
			List<string> inputs = new List<string>();
			HashSet<KeyCode> pressedDownMouse = new HashSet<KeyCode>();
			bool ShootPressed = false;
			bool preventRailcannonSpam = true;
			while (this.ReplayINT != -1)
			{
				this.ReplayINT++;
				inputs.Clear();
				string ThisFrame = "DOLF" + this.ReplayINT.ToString();
				string NextFrame = "DOLF" + (this.ReplayINT + 1).ToString();
				int num = Array.FindIndex<string>(lines, (string line) => line.Contains(ThisFrame));
				int num2 = Array.FindIndex<string>(lines, num, (string line) => line.Contains(NextFrame));
				if (num < 0 || num2 < 0)
				{
					this.ReplayINT = -1;
					break;
				}
				for (int i = num + 1; i < num2; i++)
				{
					inputs.Add(lines[i].Trim());
				}
				if (inputs.Count > 2)
				{
					MonoBehaviour.print("Frame: " + this.ReplayINT.ToString());
				}
				this.pressedDownKeys.Clear();
				this.inputActionStates.Clear();
				foreach (string text in inputs)
				{
					if (text.StartsWith("X"))
					{
						float rotationX;
						if (float.TryParse(text.Substring(1), out rotationX))
						{
							MonoSingleton<CameraController>.instance.rotationX = rotationX;
						}
					}
					else if (text.StartsWith("Y"))
					{
						float rotationY;
						if (float.TryParse(text.Substring(1), out rotationY))
						{
							MonoSingleton<CameraController>.instance.rotationY = rotationY;
						}
					}
					else if (this.PlayerActions.Contains(text) && (text != "Fire1" || text != "Fire2"))
					{
						VirtualKeyCode virtualKeyCode;
						if (this.actionMappings.TryGetValue(text, out virtualKeyCode))
						{
							this.inputActionStates.Add(virtualKeyCode);
							this.pressedSpecialKeys.Add(virtualKeyCode);
							this.SimulateSpecial(virtualKeyCode, true);
						}
						else
						{
							MonoBehaviour.print("Action has no value! " + text);
						}
					}
					else if (text == "Fire1" && MonoSingleton<GunControl>.instance.currentSlot != 4)
					{
						MonoSingleton<InputManager>.instance.InputSource.Fire1.IsPressed = true;
						MonoSingleton<InputManager>.instance.InputSource.Fire1.PerformedFrame = Time.frameCount + 1;
						ShootPressed = true;
						MonoBehaviour.print("Fire 1 is pressed");
					}
					else if (MonoSingleton<GunControl>.instance.currentSlot == 4 && text == "Fire1")
					{
						if (!preventRailcannonSpam)
						{
							preventRailcannonSpam = true;
							MonoSingleton<InputManager>.instance.InputSource.Fire1.PerformedFrame = Time.frameCount + 1;
						}
					}
					else
					{
						KeyCode? keyCodeFromInputsDic = this.GetKeyCodeFromInputsDic(text);
						KeyCode valueOrDefault = keyCodeFromInputsDic.GetValueOrDefault();
						Key? keyFromKeyCode = this.GetKeyFromKeyCode(valueOrDefault);
						if (keyFromKeyCode != null && !this.IsMouseInput(valueOrDefault))
						{
							this.pressedDownKeys.Add(keyFromKeyCode.Value);
							this.SimulateKeybord(keyFromKeyCode.Value, true);
						}
						else if (this.IsMouseInput(valueOrDefault))
						{
							pressedDownMouse.Add(valueOrDefault);
							if (valueOrDefault == 323)
							{
								MouseState mouseState = default(MouseState);
								InputDevice device = InputSystem.GetDevice<Mouse>();
								mouseState.WithButton(0, true);
								InputSystem.QueueStateEvent<MouseState>(device, mouseState, -1.0);
							}
							else if (valueOrDefault == 324)
							{
								this.SimulateMouseButton(false, true);
							}
						}
						else
						{
							MonoBehaviour.print(string.Format("Key: {0} is null or {1} is wrong or {2} is wrong", keyCodeFromInputsDic, valueOrDefault, keyFromKeyCode));
						}
					}
				}
				this.ReleaseUnpressedKeysMouseButtonsAndSpecialActions(inputs, pressedDownMouse, this.pressedSpecialKeys, ref ShootPressed, ref preventRailcannonSpam);
				yield return new WaitForEndOfFrame();
			}
			yield return new WaitForSeconds(0.05f);
			for (int j = 0; j < this.KeysStatus.Count; j++)
			{
				this.KeysStatus[j] = new ValueTuple<VirtualKeyCode, bool>(this.KeysStatus[j].Item1, false);
				this.SimulateSpecial(this.KeysStatus[j].Item1, false);
			}
			foreach (Key key in this.pressedDownKeys)
			{
				this.SimulateKeybord(key, false);
			}
			this.pressedDownKeys.Clear();
			foreach (KeyCode keyCode in pressedDownMouse)
			{
				bool left = keyCode == 323;
				this.SimulateMouseButton(left, false);
			}
			pressedDownMouse.Clear();
			this.ReplayINT = 0;
			this.PlayingTAS = false;
			MonoBehaviour.print("TAS REPLAY FINISHED");
			yield break;
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00002778 File Offset: 0x00000978
		private void ReleaseUnpressedKeysMouseButtonsAndSpecialActions(List<string> inputs, HashSet<KeyCode> pressedDownMouse, HashSet<VirtualKeyCode> pressedSpecialKeys, ref bool ShootPressed, ref bool preventRailcannonSpam)
		{
			HashSet<Key> hashSet = new HashSet<Key>();
			HashSet<VirtualKeyCode> hashSet2 = new HashSet<VirtualKeyCode>();
			foreach (string text in inputs)
			{
				KeyCode valueOrDefault = this.GetKeyCodeFromInputsDic(text).GetValueOrDefault();
				Key? keyFromKeyCode = this.GetKeyFromKeyCode(valueOrDefault);
				if (keyFromKeyCode != null && !this.IsMouseInput(valueOrDefault))
				{
					hashSet.Add(keyFromKeyCode.Value);
				}
				VirtualKeyCode item;
				if (this.actionMappings.TryGetValue(text.Trim(), out item))
				{
					hashSet2.Add(item);
				}
			}
			if (preventRailcannonSpam && !inputs.Contains("Fire1"))
			{
				preventRailcannonSpam = false;
			}
			if (ShootPressed && !inputs.Contains("Fire1"))
			{
				MonoSingleton<InputManager>.instance.InputSource.Fire1.IsPressed = false;
				ShootPressed = false;
				MonoBehaviour.print("Unpressed Fire1");
			}
			foreach (Key key in this.pressedDownKeys)
			{
				if (!hashSet.Contains(key))
				{
					MonoBehaviour.print(string.Format("Releasing key: {0}", key));
					this.SimulateKeybord(key, false);
				}
			}
			foreach (object obj in Enum.GetValues(typeof(KeyCode)))
			{
				KeyCode keyCode = (KeyCode)obj;
				if (this.IsMouseInput(keyCode) && pressedDownMouse.Contains(keyCode) && !inputs.Contains(keyCode.ToString()))
				{
					bool left = keyCode == 323;
					this.SimulateMouseButton(left, false);
				}
			}
			List<VirtualKeyCode> list = new List<VirtualKeyCode>();
			foreach (VirtualKeyCode virtualKeyCode in pressedSpecialKeys)
			{
				if (!hashSet2.Contains(virtualKeyCode))
				{
					MonoBehaviour.print(string.Format("Releasing special action: {0}", virtualKeyCode));
					this.SimulateSpecial(virtualKeyCode, false);
					list.Add(virtualKeyCode);
				}
			}
			foreach (VirtualKeyCode item2 in list)
			{
				pressedSpecialKeys.Remove(item2);
			}
		}

		// Token: 0x06000014 RID: 20 RVA: 0x00002A1C File Offset: 0x00000C1C
		private bool IsMouseInput(KeyCode key)
		{
			return key >= 323 && key <= 329;
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002A34 File Offset: 0x00000C34
		private void SimulateKeybord(Key[] keys, bool press)
		{
			KeyboardState keyboardState = default(KeyboardState);
			Keyboard device = InputSystem.GetDevice<Keyboard>();
			int i = 0;
			while (i < keys.Length)
			{
				Key key = keys[i];
				Key key2 = key;
				Key? keyFromKeyCode = this.GetKeyFromKeyCode(Class1.PauseGame.Value);
				if (key2 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null)
				{
					goto IL_B8;
				}
				Key key3 = key;
				keyFromKeyCode = this.GetKeyFromKeyCode(Class1.AdvFrame.Value);
				if (key3 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null)
				{
					goto IL_B8;
				}
				Key key4 = key;
				keyFromKeyCode = this.GetKeyFromKeyCode(Class1.PlayTAS.Value);
				if (key4 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null)
				{
					goto IL_B8;
				}
				Key key5 = key;
				keyFromKeyCode = this.GetKeyFromKeyCode(Class1.StartRecording.Value);
				if (key5 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null)
				{
					goto IL_B8;
				}
				if (device != null)
				{
					keyboardState.Set(key, press);
				}
				IL_D1:
				i++;
				continue;
				IL_B8:
				MonoBehaviour.print("Key Is assigned key so skipping");
				goto IL_D1;
			}
			InputSystem.QueueStateEvent<KeyboardState>(device, keyboardState, -1.0);
			InputSystem.Update();
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002B34 File Offset: 0x00000D34
		private void SimulateKeybord(List<Key> keys, bool press)
		{
			KeyboardState keyboardState = default(KeyboardState);
			Keyboard device = InputSystem.GetDevice<Keyboard>();
			foreach (Key key in keys)
			{
				Key key2 = key;
				Key? keyFromKeyCode = this.GetKeyFromKeyCode(Class1.PauseGame.Value);
				if (!(key2 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
				{
					Key key3 = key;
					keyFromKeyCode = this.GetKeyFromKeyCode(Class1.AdvFrame.Value);
					if (!(key3 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
					{
						Key key4 = key;
						keyFromKeyCode = this.GetKeyFromKeyCode(Class1.PlayTAS.Value);
						if (!(key4 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
						{
							Key key5 = key;
							keyFromKeyCode = this.GetKeyFromKeyCode(Class1.StartRecording.Value);
							if (!(key5 == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
							{
								if (device != null)
								{
									keyboardState.Set(key, press);
									continue;
								}
								continue;
							}
						}
					}
				}
				MonoBehaviour.print("Key Is assigned key so skipping");
			}
			InputSystem.QueueStateEvent<KeyboardState>(device, keyboardState, -1.0);
			InputSystem.Update();
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002C54 File Offset: 0x00000E54
		private void SimulateKeybord(Key key, bool press)
		{
			Key? keyFromKeyCode = this.GetKeyFromKeyCode(Class1.PauseGame.Value);
			if (!(key == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
			{
				keyFromKeyCode = this.GetKeyFromKeyCode(Class1.AdvFrame.Value);
				if (!(key == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
				{
					keyFromKeyCode = this.GetKeyFromKeyCode(Class1.PlayTAS.Value);
					if (!(key == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
					{
						keyFromKeyCode = this.GetKeyFromKeyCode(Class1.StartRecording.Value);
						if (!(key == keyFromKeyCode.GetValueOrDefault() & keyFromKeyCode != null))
						{
							goto IL_9E;
						}
					}
				}
			}
			MonoBehaviour.print("Key Is assigned key so skipping");
			IL_9E:
			Keyboard device = InputSystem.GetDevice<Keyboard>();
			if (device != null)
			{
				KeyboardState keyboardState = default(KeyboardState);
				if (press)
				{
					keyboardState..ctor(new Key[]
					{
						key
					});
				}
				else
				{
					keyboardState = default(KeyboardState);
				}
				InputSystem.QueueStateEvent<KeyboardState>(device, keyboardState, -1.0);
				InputSystem.Update();
			}
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002D44 File Offset: 0x00000F44
		private void SimulateSpecial(VirtualKeyCode input, bool press)
		{
			int num = this.KeysStatus.FindIndex((ValueTuple<VirtualKeyCode, bool> k) => k.Item1 == input);
			if (!press)
			{
				this.Simulator.Keyboard.KeyUp(input);
				return;
			}
			this.Simulator.Keyboard.KeyDown(input);
			if (num >= 0)
			{
				this.KeysStatus[num] = new ValueTuple<VirtualKeyCode, bool>(input, true);
				return;
			}
			this.KeysStatus.Add(new ValueTuple<VirtualKeyCode, bool>(input, true));
			if (num >= 0)
			{
				this.KeysStatus[num] = new ValueTuple<VirtualKeyCode, bool>(input, false);
				return;
			}
			this.KeysStatus.Add(new ValueTuple<VirtualKeyCode, bool>(input, false));
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002E14 File Offset: 0x00001014
		private void SimulateMouseButton(bool left, bool press)
		{
			Mouse device = InputSystem.GetDevice<Mouse>();
			MouseState mouseState = default(MouseState);
			if (device != null)
			{
				if (left)
				{
					if (press)
					{
						mouseState.WithButton(0, true);
						InputSystem.QueueStateEvent<MouseState>(device, mouseState, -1.0);
					}
					else
					{
						mouseState.WithButton(0, false);
						InputSystem.QueueStateEvent<MouseState>(device, mouseState, -1.0);
					}
				}
				else if (!left)
				{
					if (press)
					{
						mouseState.WithButton(1, true);
						InputSystem.QueueStateEvent<MouseState>(device, mouseState, -1.0);
					}
					else
					{
						mouseState.WithButton(1, false);
						InputSystem.QueueStateEvent<MouseState>(device, mouseState, -1.0);
					}
				}
			}
			else
			{
				Debug.LogWarning("Mouse not found.");
			}
			InputSystem.QueueStateEvent<MouseState>(device, mouseState, -1.0);
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002EC8 File Offset: 0x000010C8
		public string GetStringFromInputsDic(KeyCode input)
		{
			string result;
			if (this.dic2.TryGetValue(input, out result))
			{
				return result;
			}
			return null;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002EE8 File Offset: 0x000010E8
		public KeyCode? GetKeyCodeFromInputsDic(string input)
		{
			KeyCode value;
			if (MonoSingleton<InputManager>.Instance.inputsDictionary.TryGetValue(input, out value))
			{
				return new KeyCode?(value);
			}
			return null;
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002F1C File Offset: 0x0000111C
		public Key? GetKeyFromKeyCode(KeyCode keyCode)
		{
			Key value;
			if (this.keyMapping.TryGetValue(keyCode, out value))
			{
				return new Key?(value);
			}
			return null;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002F4C File Offset: 0x0000114C
		private void translateMapping(string input, out VirtualKeyCode? action)
		{
			VirtualKeyCode value;
			if (this.actionMappings.TryGetValue(input, out value))
			{
				action = new VirtualKeyCode?(value);
			}
			action = null;
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002F7C File Offset: 0x0000117C
		public static VirtualKeyCode? GetVirtualKeyCode(KeyCode key)
		{
			VirtualKeyCode value;
			if (Class1.unityKeyCodeToVirtualKeyCode.TryGetValue(key, out value))
			{
				return new VirtualKeyCode?(value);
			}
			return null;
		}

		// Token: 0x04000001 RID: 1
		private static ConfigBuilder cfgB;

		// Token: 0x04000002 RID: 2
		private bool CaptureInputs;

		// Token: 0x04000003 RID: 3
		private static string FileSavePath;

		// Token: 0x04000004 RID: 4
		private string CurrentTASFile;

		// Token: 0x04000005 RID: 5
		[Configgable("", "Tas NAME", 1, "IF YOU DONT PUT A NAME OLD TAS's OF THE SAME NAME WILL BE OVERWRITTEN")]
		private static ConfigInputField<string> TasName = new ConfigInputField<string>("READ DESCRIPTION", null, null);

		// Token: 0x04000006 RID: 6
		[Configgable("Keybinds", "Start Recoring", 0, null)]
		private static ConfigInputField<KeyCode> StartRecording = new ConfigInputField<KeyCode>(107, null, null);

		// Token: 0x04000007 RID: 7
		[Configgable("Keybinds", "Pause Game", 0, null)]
		private static ConfigInputField<KeyCode> PauseGame = new ConfigInputField<KeyCode>(112, null, null);

		// Token: 0x04000008 RID: 8
		[Configgable("Keybinds", "Advance Frame", 0, null)]
		private static ConfigInputField<KeyCode> AdvFrame = new ConfigInputField<KeyCode>(111, null, null);

		// Token: 0x04000009 RID: 9
		[Configgable("Keybinds", "Play TAS", 0, null)]
		private static ConfigInputField<KeyCode> PlayTAS = new ConfigInputField<KeyCode>(109, null, null);

		// Token: 0x0400000A RID: 10
		[Configgable("TAS Replay", "TAS Name", 1, "")]
		private static ConfigDropdown<string> TasReplayName;

		// Token: 0x0400000B RID: 11
		[Configgable("TAS Replay", "TAS Name", 1, "")]
		private static ConfigButton Refr = new ConfigButton(new Action(Class1.RefreshTASList), "Refresh TAS List");

		// Token: 0x0400000C RID: 12
		private static string[] TASList;

		// Token: 0x0400000D RID: 13
		private InputSimulator Simulator;

		// Token: 0x0400000E RID: 14
		public static bool wasTSUsedThisScene = false;

		// Token: 0x0400000F RID: 15
		private static List<string> TempTAS = new List<string>();

		// Token: 0x04000010 RID: 16
		private int frame;

		// Token: 0x04000011 RID: 17
		private readonly List<string> PlayerActions = new List<string>
		{
			"Jump",
			"Dodge",
			"Slide",
			"Punch",
			"Hook",
			"LastUsedWeapon",
			"ChangeVariation",
			"ChangeFist",
			"Slot1",
			"Slot2",
			"Slot3",
			"Slot4",
			"Slot5",
			"Slot6"
		};

		// Token: 0x04000012 RID: 18
		private Text status;

		// Token: 0x04000013 RID: 19
		private bool LoopRunning;

		// Token: 0x04000014 RID: 20
		private bool TimePaused;

		// Token: 0x04000015 RID: 21
		private bool TimePausedUndone = true;

		// Token: 0x04000016 RID: 22
		private float prevTimeScale = 1f;

		// Token: 0x04000017 RID: 23
		private int ReplayINT;

		// Token: 0x04000018 RID: 24
		private bool PlayingTAS;

		// Token: 0x04000019 RID: 25
		private List<Key> pressedDownKeys = new List<Key>();

		// Token: 0x0400001A RID: 26
		private List<VirtualKeyCode> inputActionStates = new List<VirtualKeyCode>();

		// Token: 0x0400001B RID: 27
		private HashSet<VirtualKeyCode> pressedSpecialKeys = new HashSet<VirtualKeyCode>();

		// Token: 0x0400001C RID: 28
		private List<ValueTuple<VirtualKeyCode, bool>> KeysStatus = new List<ValueTuple<VirtualKeyCode, bool>>();

		// Token: 0x0400001D RID: 29
		private Dictionary<KeyCode, string> dic2 = new Dictionary<KeyCode, string>();

		// Token: 0x0400001E RID: 30
		private Dictionary<string, VirtualKeyCode> actionMappings = new Dictionary<string, VirtualKeyCode>();

		// Token: 0x0400001F RID: 31
		private static readonly Dictionary<KeyCode, VirtualKeyCode> unityKeyCodeToVirtualKeyCode = new Dictionary<KeyCode, VirtualKeyCode>
		{
			{
				97,
				65
			},
			{
				98,
				66
			},
			{
				99,
				67
			},
			{
				100,
				68
			},
			{
				101,
				69
			},
			{
				102,
				70
			},
			{
				103,
				71
			},
			{
				104,
				72
			},
			{
				105,
				73
			},
			{
				106,
				74
			},
			{
				107,
				75
			},
			{
				108,
				76
			},
			{
				109,
				77
			},
			{
				110,
				78
			},
			{
				111,
				79
			},
			{
				112,
				80
			},
			{
				113,
				81
			},
			{
				114,
				82
			},
			{
				115,
				83
			},
			{
				116,
				84
			},
			{
				117,
				85
			},
			{
				118,
				86
			},
			{
				119,
				87
			},
			{
				120,
				88
			},
			{
				121,
				89
			},
			{
				122,
				90
			},
			{
				48,
				48
			},
			{
				49,
				49
			},
			{
				50,
				50
			},
			{
				51,
				51
			},
			{
				52,
				52
			},
			{
				53,
				53
			},
			{
				54,
				54
			},
			{
				55,
				55
			},
			{
				56,
				56
			},
			{
				57,
				57
			},
			{
				13,
				13
			},
			{
				27,
				27
			},
			{
				8,
				8
			},
			{
				9,
				9
			},
			{
				32,
				32
			},
			{
				273,
				38
			},
			{
				274,
				40
			},
			{
				276,
				37
			},
			{
				275,
				39
			},
			{
				304,
				160
			},
			{
				303,
				161
			},
			{
				306,
				162
			},
			{
				305,
				163
			}
		};

		// Token: 0x04000020 RID: 32
		private Dictionary<KeyCode, Key> keyMapping = new Dictionary<KeyCode, Key>
		{
			{
				97,
				15
			},
			{
				98,
				16
			},
			{
				99,
				17
			},
			{
				100,
				18
			},
			{
				101,
				19
			},
			{
				102,
				20
			},
			{
				103,
				21
			},
			{
				104,
				22
			},
			{
				105,
				23
			},
			{
				106,
				24
			},
			{
				107,
				25
			},
			{
				108,
				26
			},
			{
				109,
				27
			},
			{
				110,
				28
			},
			{
				111,
				29
			},
			{
				112,
				30
			},
			{
				113,
				31
			},
			{
				114,
				32
			},
			{
				115,
				33
			},
			{
				116,
				34
			},
			{
				117,
				35
			},
			{
				118,
				36
			},
			{
				119,
				37
			},
			{
				120,
				38
			},
			{
				121,
				39
			},
			{
				122,
				40
			},
			{
				48,
				50
			},
			{
				49,
				41
			},
			{
				50,
				42
			},
			{
				51,
				43
			},
			{
				52,
				44
			},
			{
				53,
				45
			},
			{
				54,
				46
			},
			{
				55,
				47
			},
			{
				56,
				48
			},
			{
				57,
				49
			},
			{
				32,
				1
			},
			{
				13,
				2
			},
			{
				27,
				60
			},
			{
				8,
				65
			},
			{
				9,
				3
			},
			{
				304,
				51
			},
			{
				303,
				52
			},
			{
				306,
				55
			},
			{
				305,
				56
			},
			{
				308,
				53
			},
			{
				307,
				54
			},
			{
				273,
				63
			},
			{
				274,
				64
			},
			{
				276,
				61
			},
			{
				275,
				62
			}
		};
	}
}
