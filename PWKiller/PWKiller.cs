using System;
using System.Windows.Forms;
using PWKiller.Core;

namespace PWKiller;

/// <summary>The window of the user mode: one button that ends the game.</summary>
public partial class PWKillerMain : Form
{
    private readonly KillerActions m_actions;

    public PWKillerMain(KillerActions p_Actions, KillerTexts p_Texts)
    {
        m_actions = p_Actions;
        InitializeComponent();
        PWKillerTitleText.Text = p_Texts.Title;
        PWKillerButtonText.Text = p_Texts.Button;
    }

    private void MainButton_Click(object? p_Sender, EventArgs p_Args)
    {
        PWKillerButtonText.Enabled = false;
        UseWaitCursor = true;
        try
        {
            m_actions.KillGame();
        }
        finally
        {
            Close();
        }
    }
}