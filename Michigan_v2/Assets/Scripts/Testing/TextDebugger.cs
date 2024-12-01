using BeauRoutine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextDebugger : MonoBehaviour
{
    const string alertColor = "#1a7838";
    const string warnColor = "#94781e";
    const string errorColor = "#942b1e";

    [SerializeField]
    TextMeshProUGUI outputField = null;
    [SerializeField]
    bool richText = true;

    private static TextDebugger instance;
    private static Routine writer;
    private static List<Message> writeQueue = new List<Message>();
    private static bool clearing = false;

    public enum MessageType
    {
        Log,
        Warn,
        Error,
        Alert
    }

    public struct Message
    {
        public string message;
        public MessageType messageType;
    }

    private void Awake()
    {
        if (instance) Destroy(instance);
        instance = this;
    }

    static IEnumerator WriteQueue()
    {
        while (!clearing && writeQueue.Count > 0)
        {
            var up = writeQueue[0];
            writeQueue.RemoveAt(0);

            string current = instance.outputField.text;
            current += FormatString(up.message, up.messageType);
            instance.outputField.text = System.Text.RegularExpressions.Regex.Unescape(current);
            GUIUtility.systemCopyBuffer = instance.outputField.text;

            yield return null;
        }
    }

    static string FormatString(string inMsg, MessageType inType)
    {
        string output = "";

        switch (inType)
        {
            case MessageType.Alert:
                if (instance?.richText ?? false) output = $"<color {alertColor}>{inMsg}</color>";
                else output = $"** {inMsg} **";
                Debug.Log(inMsg);
                break;
            case MessageType.Warn:
                if (instance?.richText ?? false) output = $"<color {warnColor}>{inMsg}</color>";
                else output = $"// {inMsg} //";
                Debug.LogWarning(inMsg);
                break;
            case MessageType.Error:
                if (instance?.richText ?? false) output = $"<color {errorColor}>{inMsg}</color>";
                else output = $"!! {inMsg} !!";
                Debug.LogError(inMsg);
                break;
            default:
                output = inMsg;
                Debug.Log(inMsg);
                break;
        }

        return output + "\n";
    }

    public static void Clear()
    {
        if (instance?.outputField)
        {
            clearing = true;
            writeQueue.Clear();
            if (writer.Exists()) writer.Stop();

            instance.outputField.text = "";
            clearing = false;
        }
    }

    public static void Log(string inMsg)
    {
        if (instance?.outputField)
        {
            writeQueue.Add(new Message { message = inMsg });
            if (!writer.Exists()) writer.Replace(WriteQueue());
        }
        else Debug.Log(inMsg);
    }

    public static void Warn(string inMsg)
    {
        if (instance?.outputField)
        {
            writeQueue.Add(new Message { message = inMsg, messageType = MessageType.Warn });
            if (!writer.Exists()) writer.Replace(WriteQueue());
        }
        else Debug.LogWarning(inMsg);
    }

    public static void Error(string inMsg)
    {
        if (instance?.outputField)
        {
            writeQueue.Add(new Message { message = inMsg, messageType = MessageType.Error });
            if (!writer.Exists()) writer.Replace(WriteQueue());
        }
        else Debug.LogError(inMsg);
    }

    public static void Alert(string inMsg)
    {
        if (instance?.outputField)
        {
            writeQueue.Add(new Message { message = inMsg, messageType = MessageType.Alert });
            if (!writer.Exists()) writer.Replace(WriteQueue());
        }
        else Debug.Log("** " + inMsg + " **");
    }
}
