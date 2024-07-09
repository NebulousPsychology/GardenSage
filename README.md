# GardenSage

Tools for collecting forecasts and determining when outdoor chores are most viable

- <https://open-meteo.com/en/docs>
- <https://github.com/open-meteo/sdk/>
- <https://github.com/open-meteo/dotnet/>

## Sample data

lat/long samples attempt to use the Space Needle: 47.620529, -122.349297

But, openmeteo returns results for some Amazon offices nearby; defaults will bow to this site

    - latitude: `47.621212`
    - longitude: `-122.33495`

## Synthetic temp math

the fundamental wavelengths are $y(t)$ and $d(t)$

$$
\begin{align}
    k_{\lambda_{h}} &= {2\pi}\\
    k_\lambda &= \frac {k_{\lambda_h}}{24}\\
    d(t) &= -\cos\bigg(\frac {2\pi \cdot t} {24}\bigg) \\
        &= -\cos\bigg(k_\lambda t\bigg) \\
    y(t,\phi=0) &= -\cos\bigg(\frac {2\pi \cdot t} {365 \cdot 24}\bigg) \\
        &= -\cos\bigg(\frac{k_\lambda t}{365} \bigg) \\
\end{align}
$$

### daily amplitude modulation

$k_{\alpha}$ is the constant which drives the change in daily amplitude across the seasons,
so summer varies wider than winter.

$$
\begin{align}
k_{\alpha}
    = 7
    &= \frac{S_{max}-S_{min}}{2} - \frac{W_{max}-W_{min}}{2} \\
    &= \frac{1}{2}  (S_{max}-S_{min} -W_{max} +W_{min}) \checkmark
\end{align}
$$

$\alpha_d(t)$ is the equation for daily amplitude modulation across the seasons,
so summer varies wider than winter

$$
\begin{align}
\alpha_d(t) &= \frac {7}{2} y(t) + 10.5 \\
            &= \frac {k_{\alpha}}{2} y(t) + (k_{\alpha}*1.5 ) \\
\alpha_d(t) &= k_{\alpha} \bigg(\frac {y(t)}{2} + 1.5 \bigg ) \checkmark
\end{align}
$$

$T_{d_\alpha}(t)$ is the temperature's daily modulation wave

$$
\begin{align}
 T_{d_\alpha}(t)= \alpha_d(t) \cdot d(t)
\end{align}
$$

### seasonal carrier wave

$\alpha_{\bar{y}}$ is the amplitude constant for the year-long carrier wave

$$
\begin{align}
\alpha_{\bar{y}}
    &= 23.5 = \delta_{\bar{season}} / 2 = \frac{\bar{S}-\bar{W}}{2} \\
    &= \frac{1}{2} \bigg( \frac{S_{max}+S_{min}}{2} - \frac{W_{max}+W_{min}}{2} \bigg) \\
    &= \frac{1}{4} \bigg( S_{max}+S_{min} - W_{max} -W_{min} \bigg) \checkmark
\end{align}
$$

$c_{\bar{y}}$ is the average across the year, the carrier wave's incercept

$$
\begin{align}
c_{\bar{y}} &= 56.5
            = \frac{1}{2} (\bar{S} + \bar{W}) \\
            &= \frac{1}{2} \bigg( \frac{S_{max}+S_{min}}{2} + \frac{W_{max}+W_{min}}{2} \bigg) \\
c_{\bar{y}} &= \frac{1}{4} \bigg( S_{max}+S_{min} + W_{max}+W_{min} \bigg) \checkmark
\end{align}
$$

$T_{\bar{y}}(t)$ is the carrier wave for the year

$$
\begin{align}
T_{\bar{y}}(t) &= 23.5 \cdot y(t) + 56.5 \\
T_{\bar{y}}(t) &= \alpha_{\bar{y}} \cdot y(t) + c_{\bar{y}} \checkmark \\
\end{align}
$$

### modulated wave

$T(t)$ is the year' carrier wave, amplitude-modulated with the daily wave

$$
\begin{align}
T(t) &=  T_{\bar{y}}(t) + T_{d_\alpha}(t) \\
T(t) &=  T_{\bar{y}}(t) + \alpha_d(t) \cdot d(t) \checkmark
\end{align}
$$

The final waveform is further perturbed with a minor noise wave

$$
\begin{align}
T_+(t)
    &= 3\sin(t) + T(t)  \\
    &= 3\sin(t) + T_{\bar{y}}(t) + T_{d_\alpha}(t)   \\
    &= 3\sin(t) + T_{\bar{y}}(t) + \alpha_d(t) \cdot d(t) \\
\end{align}
$$
