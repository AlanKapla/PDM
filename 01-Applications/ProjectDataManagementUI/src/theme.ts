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
    // Neutralne (ciepłe szarości — tło dashboardu, obramowania, tekst pomocniczy)
    neutral: {
      25:  "#F8F7F4",
      50:  "#F1EFE8",
      100: "#D3D1C7",
      200: "#E8E6DF",
      300: "#B4B2A9",
      400: "#888780",
      500: "#75736C",
      600: "#5F5E5A",
      700: "#45443F",
      800: "#2E2D29",
      900: "#1C1B18",
    },
    // Bursztynowy (ostrzeżenia — semantycznie oddzielna od coral/orange)
    amber: {
      50:  "#FAEEDA",
      400: "#BA7517",
      600: "#854F0B",
    },
    // Orange — jawna rejestracja wbudowanej palety Chakra (self-contained theme)
    orange: {
      50:  "#FFFAF0",
      100: "#FEEBC8",
      200: "#FBD38D",
      300: "#F6AD55",
      400: "#ED8936",
      500: "#DD6B20",
      600: "#C05621",
      700: "#9C4221",
      800: "#7B341E",
      900: "#652B19",
    },
  },
  components: {
    Badge: {
      baseStyle: {
        borderRadius: "full",
        fontWeight: "medium",
        textTransform: "none",
        letterSpacing: "normal",
      },
      defaultProps: {
        variant: "subtle",
      },
    },
  },
});

export default theme;
