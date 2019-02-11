using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExercicesManager : MonoBehaviour {

    //Grid
    public GameObject PanelGrid;

    //Exercices
    public GameObject Exercice1;
    public GameObject Exercice2;
    public GameObject Exercice3;
    public GameObject Exercice4;
    public GameObject Exercice5;
    public GameObject Exercice6;
    public GameObject Exercice7;
    public GameObject Exercice8;
    public GameObject Exercice9;
    public GameObject Exercice10;
    public GameObject Exercice11;
    public GameObject Exercice12;
    public GameObject PanelExercices;

    //Form
    public GameObject PanelForm;
    public InputField InputFieldSeries;
    public InputField InputFieldRepetitions;
    public GameObject TextControlSeries;
    public GameObject TextControlRepetitions;
    public Button ButtonClose; 

    private GameObject ActualExercice;
    private int ExerciceNb; 


    // Use this for initialization
    void Start () {
        PanelGrid.SetActive(true);
        PanelExercices.SetActive(false);
        PanelForm.SetActive(false);

        //Form
        ButtonClose.enabled = false;

        DataController.Instance.LoadUserData();
        
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void DisplayExercice(int ExerciceNb)
    {
        PanelGrid.SetActive(false);
        PanelExercices.SetActive(true);

        switch (ExerciceNb)
        {
            case 1:
                Exercice1.SetActive(true);
                ActualExercice = Exercice1;
                ExerciceNb = 1;
                break;
            case 2:
                Exercice2.SetActive(true);
                ActualExercice = Exercice2;
                ExerciceNb = 2;
                break;
            case 3:
                Exercice3.SetActive(true);
                ActualExercice = Exercice3;
                ExerciceNb = 3;
                break;
            case 4:
                Exercice4.SetActive(true);
                ActualExercice = Exercice4;
                ExerciceNb = 4;
                break;
            case 5:
                Exercice5.SetActive(true);
                ActualExercice = Exercice5;
                ExerciceNb = 5;
                break;
            case 6:
                Exercice6.SetActive(true);
                ActualExercice = Exercice6;
                ExerciceNb = 6;
                break;
            case 7:
                Exercice7.SetActive(true);
                ActualExercice = Exercice7;
                ExerciceNb = 7;
                break;
            case 8:
                Exercice8.SetActive(true);
                ActualExercice = Exercice8;
                ExerciceNb = 8;
                break;
            case 9:
                Exercice9.SetActive(true);
                ActualExercice = Exercice9;
                ExerciceNb = 9;
                break;
            case 10:
                Exercice10.SetActive(true);
                ActualExercice = Exercice10;
                ExerciceNb = 10;
                break;
            case 11:
                Exercice11.SetActive(true);
                ActualExercice = Exercice11;
                ExerciceNb = 11;
                break;
            case 12:
                Exercice12.SetActive(true);
                ActualExercice = Exercice12;
                ExerciceNb = 12;
                break;
            default:
                print("Incorrect Exercice number");
                break;
        }
    }

    public void CloseExercice()
    {      
        ActualExercice.SetActive(false);
        PanelForm.SetActive(true);
    }

    public void CloseForm()
    {
        Exercice exercice = new Exercice(ExerciceNb, int.Parse(InputFieldRepetitions.text), int.Parse(InputFieldSeries.text));
        UserData ud = DataController.Instance.GetUserData();
        ud.Exercices.Add(exercice);
        DataController.Instance.SaveUserData();
        InputFieldSeries.text = "";
        InputFieldRepetitions.text = "";
        ButtonClose.enabled = false;
        PanelForm.SetActive(false);
        PanelGrid.SetActive(true);
    }

    public void CheckFields()
    {
        bool checkSeries = false;
        bool checkRep = false;

        //Check Series InputField
        if (int.Parse(InputFieldSeries.text) >= 1 && int.Parse(InputFieldSeries.text) <= 20)
        {
            TextControlSeries.SetActive(false);
            checkSeries = true;
        }
        else
        {
            TextControlSeries.SetActive(true);
            checkSeries = false;
        }

        //Check Repetitions InputField
        if (int.Parse(InputFieldRepetitions.text) >= 1 && int.Parse(InputFieldRepetitions.text) <= 20)
        {
            TextControlRepetitions.SetActive(false);
            checkRep = true;
        }
        else
        {
            TextControlRepetitions.SetActive(true);
            checkRep = false;
        }

        
        if (checkSeries && checkRep)
        {
            ButtonClose.enabled = true;
        }
        else
        {
            ButtonClose.enabled = false;
        }

    }
}
