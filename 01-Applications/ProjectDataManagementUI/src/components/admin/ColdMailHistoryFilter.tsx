import React from "react";
import { FormControl, Input, InputGroup, InputLeftElement } from "@chakra-ui/react";
import { Search } from "lucide-react";

export interface ColdMailHistoryFilterProps {
  value: string;
  onChange: (value: string) => void;
}

export function ColdMailHistoryFilter({
  value,
  onChange,
}: ColdMailHistoryFilterProps): React.ReactElement {
  return (
    <FormControl maxW="400px">
      <InputGroup size="sm">
        <InputLeftElement pointerEvents="none">
          <Search size={14} aria-hidden="true" />
        </InputLeftElement>
        <Input
          value={value}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            onChange(e.target.value)
          }
          placeholder="Filtruj po e-mailu odbiorcy..."
          aria-label="Filtruj historię po adresie e-mail odbiorcy"
        />
      </InputGroup>
    </FormControl>
  );
}
