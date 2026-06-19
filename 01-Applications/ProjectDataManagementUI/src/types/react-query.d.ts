import "@tanstack/react-query";

declare module "@tanstack/react-query" {
  interface Register {
    mutationMeta: {
      /** Gdy true — globalny handler mutacji nie pokazuje toastu */
      silent?: boolean;
    };
    queryMeta: {
      silent?: boolean;
    };
  }
}
