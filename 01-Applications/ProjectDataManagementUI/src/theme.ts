import { extendTheme } from "@chakra-ui/react";
import type { ThemeConfig } from "@chakra-ui/react";
import { primaryColors, level1Colors, level2Colors, actionColors } from "./theme/tokens/colors";

const config: ThemeConfig = {
  initialColorMode: "light",
  useSystemColorMode: false,
};

const theme = extendTheme({
  config,
  colors: {
    // Kolor przewodni – niebieski (rejestrujemy jako "primary" obok standardowych Chakra)
    primary: primaryColors,
    // Hierarchia poziom 1 – zielony
    level1: level1Colors,
    // Hierarchia poziom 2 – fioletowy
    level2: level2Colors,
    // Akcje drugorzędne – teal
    action: actionColors,
  },
});

export default theme;
