using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using SimpleJSON;
using System;

public class ChatController : MonoBehaviour
{
    [SerializeField]
    Dropdown drdModel;
    [SerializeField]
    InputField txtContent;
    [SerializeField]
    Text txtResponse, txtCopyInfo;
    [SerializeField]
    GameObject loader1, loader2;

    string replicate_token = "your-replicate-token";
    bool isCancelled = false;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator GetOpenAIResponse(string content)
    {
        UnityWebRequest request = new UnityWebRequest("https://YOUR_RESOURCE_NAME.openai.azure.com/openai/deployments/YOUR_DEPLOYMENT_NAME/chat/completions?api-version=2023-05-15", "POST");
        string body = "{\"messages\": [{\"role\": \"system\",\"content\": \"You are a helpful assistant\"},{ \"role\": \"user\",\"content\": \""+ content + "\"}]}";
        byte[] data = new System.Text.UTF8Encoding().GetBytes(body);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data); 
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("api-key","your-azure-openai-key");

        yield return request.SendWebRequest();

        if (request.isNetworkError)
            Debug.Log("Error While Sending: " + request.error);
        // else
        //     Debug.Log("Received: " + request.downloadHandler.text);
        JSONNode jsonNode = JSON.Parse(request.downloadHandler.text);
        string response = jsonNode["choices"][0]["message"]["content"].ToString().Trim('"');
        loader1.SetActive(false);
        loader2.SetActive(false);
        txtResponse.text = response;
    }

    IEnumerator GetReplicateModelResponse(string version, string token, string content)
    {
        UnityWebRequest request = new UnityWebRequest("https://api.replicate.com/v1/predictions", "POST");
        string body = "{\"version\": \""+ version + "\", \"input\": {\"prompt\": \"" + content + "\"}}";
        byte[] data = new System.Text.UTF8Encoding().GetBytes(body);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data); 
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization","Token " + token);

        yield return request.SendWebRequest();

        if (request.isNetworkError)
            Debug.Log("Error While Sending: " + request.error);
        // else
        //     Debug.Log("Received: " + request.downloadHandler.text);
        JSONNode jsonNode = JSON.Parse(request.downloadHandler.text);

        string status = jsonNode["status"].ToString().Trim('"');
        string id = jsonNode["id"].ToString().Trim('"');
        string url = jsonNode["urls"][isCancelled ? "cancel" : "get"].ToString().Trim('"');
        
        if(status == "starting") {
            StartCoroutine(RefetchAPI(url, token));
        }else if(status == "succeeded") {
            txtResponse.text = ""; 
            loader1.SetActive(false);
            loader2.SetActive(false);
            for(int i=0;i<jsonNode["output"].Count;i++)
                txtResponse.text += jsonNode["output"][i];
        }
    }

    IEnumerator RefetchAPI(string url, string token)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization","Token " + token);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.isNetworkError)
            Debug.Log("Error While Sending: " + request.error);
        // else
        //     Debug.Log("Received: " + request.downloadHandler.text);
        JSONNode jsonNode = JSON.Parse(request.downloadHandler.text);
        
        string status = jsonNode["status"].ToString().Trim('"');
        string id = jsonNode["id"].ToString().Trim('"');
        string get_url = jsonNode["urls"][isCancelled ? "cancel" : "get"].ToString().Trim('"');
        
        if(status == "starting" || status == "processing") {
            StartCoroutine(RefetchAPI(get_url, token));
        }else if(status == "succeeded") {
            txtResponse.text = ""; 
            loader1.SetActive(false);
            loader2.SetActive(false);
            for(int i=0;i<jsonNode["output"].Count;i++)
                txtResponse.text += jsonNode["output"][i];
        }
    }

    public void GetResponse() {
        
        loader1.SetActive(true);
        loader2.SetActive(true);
        
        isCancelled = false;
        txtResponse.text = "";
        string model = drdModel.options[drdModel.value].text;
        
        switch(model) {
            case "openai": StartCoroutine(GetOpenAIResponse(txtContent.text)); break;
            case "zephyr-7b-alpha": StartCoroutine(GetReplicateModelResponse("14ec63365a1141134c41b652fe798633f48b1fd28b356725c4d8842a0ac151ee", replicate_token, txtContent.text)); break;
            case "mistral-7b-instruct-v0.1": StartCoroutine(GetReplicateModelResponse("83b6a56e7c828e667f21fd596c338fd4f0039b46bcfa18d973e8e70e455fda70", replicate_token, txtContent.text)); break;
            case "falcon-40b-instruct": StartCoroutine(GetReplicateModelResponse("7eb0f4b1ff770ab4f68c3a309dd4984469749b7323a3d47fd2d5e09d58836d3c", replicate_token, txtContent.text)); break;
            case "llama-2-13b": StartCoroutine(GetReplicateModelResponse("078d7a002387bd96d93b0302a4c03b3f15824b63104034bfa943c63a8f208c38", replicate_token, txtContent.text)); break;
            case "vicuna-13b": StartCoroutine(GetReplicateModelResponse("6282abe6a492de4145d7bb601023762212f9ddbbe78278bd6771c8b3b2f2a13b", replicate_token, txtContent.text)); break;
            case "codellama-13b-instruct": StartCoroutine(GetReplicateModelResponse("4d4dfb567b910309c9501d56807864fc069ffcd2867552aea073c4b374eef309", replicate_token, txtContent.text)); break;
            default: txtContent.text = "Wrong model selection"; break;
        }
    }

    public void CancelRequest() {
        isCancelled = true;
        loader1.SetActive(false);
        loader2.SetActive(false);
    }

    public void CopyToClipboard() {
        GUIUtility.systemCopyBuffer = txtResponse.text;
        txtCopyInfo.text = "Copied";
        StartCoroutine(Delay(3,()=>{
            txtCopyInfo.text = "Copy";
        }));
    }

    IEnumerator Delay(int sec, Action callback) {
        yield return new WaitForSeconds(sec);
        callback.Invoke();
    }

}
