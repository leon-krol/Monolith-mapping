// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto
// SPDX-FileCopyrightText: 2022 Leon Friedrich
// SPDX-FileCopyrightText: 2022 Paul Ritter
// SPDX-FileCopyrightText: 2022 mirrorcult
// SPDX-FileCopyrightText: 2022 wrexbe
// SPDX-FileCopyrightText: 2025 starch
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using Content.Client.UserInterface.Controls;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Input;
using Range = Robust.Client.UserInterface.Controls.Range;

namespace Content.Client.Atmos.UI
{
    /// <summary>
    /// Client-side UI used to control a canister.
    /// </summary>
    [GenerateTypedNameReferences]
    public sealed partial class GasCanisterWindow : FancyWindow
    {
        private readonly ButtonGroup _buttonGroup = new();

        public event Action? TankEjectButtonPressed;
        public event Action<float>? ReleasePressureSet;
        public event Action? ReleaseValveCloseButtonPressed;
        public event Action? ReleaseValveOpenButtonPressed;

        public GasCanisterWindow()
        {
            RobustXamlLoader.Load(this);

            ReleaseValveCloseButton.Group = _buttonGroup;
            ReleaseValveOpenButton.Group = _buttonGroup;

            ReleaseValveCloseButton.OnPressed += _ => ReleaseValveCloseButtonPressed?.Invoke();
            ReleaseValveOpenButton.OnPressed += _ => ReleaseValveOpenButtonPressed?.Invoke();

            TankEjectButton.OnPressed += _ => TankEjectButtonPressed?.Invoke();
            ReleasePressureSlider.OnKeyBindUp += OnReleasePressureSliderReleased;
            ReleasePressureSlider.OnValueChanged += OnReleasePressureSliderChanged;
            ReleasePressure.OnValueChanged += OnReleasePressureChanged;
        }

        private void OnReleasePressureChanged(FloatSpinBox.FloatSpinBoxEventArgs args)
        {
            var value = Math.Clamp(args.Value, ReleasePressureSlider.MinValue, ReleasePressureSlider.MaxValue);

            ReleasePressureSlider.SetValueWithoutEvent(value);
            ReleasePressureSet?.Invoke(value);
        }

        private void OnReleasePressureSliderChanged(Range range)
        {
            ReleasePressure.Value = range.Value;
        }

        private void OnReleasePressureSliderReleased(GUIBoundKeyEventArgs args)
        {
            if (args.Function != EngineKeyFunctions.UIClick)
                return;

            ReleasePressureSet?.Invoke(ReleasePressureSlider.Value);
        }

        public void SetCanisterLabel(string label)
        {
            Title = label;
        }

        public void SetCanisterPressure(float pressure)
        {
            CanisterPressureLabel.Text = Loc.GetString("comp-gas-canister-ui-pressure", ("pressure", Math.Round(pressure)));
        }

        public void SetPortStatus(bool status)
        {
            if (status)
            {
                PortStatusLabel.Text = Loc.GetString("comp-gas-canister-ui-port-connected");
                PortStatusLabel.FontColorOverride = Color.Green;
            }
            else
            {
                PortStatusLabel.Text = Loc.GetString("comp-gas-canister-ui-port-disconnected");
                PortStatusLabel.FontColorOverride = Color.Red;
            }
        }

        public void SetTankLabel(string? label)
        {
            if (label == null)
            {
                TankEjectButton.Disabled = true;
                TankLabelLabel.Text = Loc.GetString("comp-gas-canister-ui-holding-tank-label-empty");
                return;
            }

            TankEjectButton.Disabled = false;
            TankLabelLabel.Text = label;
        }

        public void SetTankPressure(float pressure)
        {
            TankPressureLabel.Text = Loc.GetString("comp-gas-canister-ui-pressure", ("pressure", Math.Round(pressure)));
        }

        public void SetReleasePressureRange(float min, float max)
        {
            ReleasePressureSlider.MinValue = min;
            ReleasePressureSlider.MaxValue = max;
        }

        public void SetReleasePressure(float pressure)
        {
            if (MathHelper.CloseTo(pressure, ReleasePressure.Value))
                return;

            if(!ReleasePressureSlider.Grabbed)
                ReleasePressureSlider.SetValueWithoutEvent(pressure);
            ReleasePressure.Value = pressure;
        }

        public void SetReleaseValve(bool valve)
        {
            if (valve)
                ReleaseValveOpenButton.Pressed = true;
            else
                ReleaseValveCloseButton.Pressed = true;
        }
    }
}
