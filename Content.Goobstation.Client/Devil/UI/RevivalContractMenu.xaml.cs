// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Goobstation.Client.Devil.UI;

[GenerateTypedNameReferences]
public sealed partial class RevivalContractMenu : DefaultWindow
{
    public event Action? Accepted;
    public event Action? Rejected;

    public EntityUid? Entity { get; private set; }

    public RevivalContractMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        AcceptButton.OnPressed += OnAcceptPressed;
        RejectButton.OnPressed += OnRejectPressed;
    }

    private void OnAcceptPressed(BaseButton.ButtonEventArgs args)
    {
        Accepted?.Invoke();
        Close();
    }

    private void OnRejectPressed(BaseButton.ButtonEventArgs args)
    {
        Rejected?.Invoke();
        Close();
    }

    public void SetEntity(EntityUid ent)
    {
        Entity = ent;
    }
}
