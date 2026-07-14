import React from 'react';
import { Checkbox, Stack, Text } from '@chakra-ui/react';
import {
  ProjectModule,
  PROJECT_MODULE_LABELS,
  DEFAULT_INVITE_PROJECT_MODULES,
} from '../types/projectModulePermissions';

const SELECTABLE_MODULES = (Object.values(ProjectModule) as number[]).filter(
  (m) => m !== ProjectModule.Settings
);

export interface ProjectModulePermissionPickerProps {
  selectedModules: Set<number>;
  onChange: (modules: Set<number>) => void;
  isDisabled?: boolean;
}

export function ProjectModulePermissionPicker({
  selectedModules,
  onChange,
  isDisabled = false,
}: ProjectModulePermissionPickerProps): React.ReactElement {
  return (
    <Stack spacing={2}>
      <Text fontSize="xs" fontWeight="semibold" color="neutral.500">
        UPRAWNIENIA DO MODUŁÓW
      </Text>
      {SELECTABLE_MODULES.map((mod) => (
        <Checkbox
          key={mod}
          isChecked={selectedModules.has(mod)}
          isDisabled={isDisabled}
          onChange={(e) => {
            onChange(
              (() => {
                const next = new Set(selectedModules);
                if (e.target.checked) {
                  next.add(mod);
                } else {
                  next.delete(mod);
                }
                return next;
              })()
            );
          }}
        >
          {PROJECT_MODULE_LABELS[mod as ProjectModule]}
        </Checkbox>
      ))}
    </Stack>
  );
}

export function createDefaultInviteModulesSet(): Set<number> {
  return new Set(DEFAULT_INVITE_PROJECT_MODULES);
}
