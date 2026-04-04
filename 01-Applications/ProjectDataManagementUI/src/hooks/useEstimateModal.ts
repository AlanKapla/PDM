import { useState, useCallback } from 'react';

export type ModalElementType = 'group' | 'item';

interface ModalState {
  isOpen: boolean;
  elementType: ModalElementType | null;
  groupId: string | null;
  groupNumber: string;
  itemId: string | null;
  itemNumber: number;
}

const CLOSED: ModalState = {
  isOpen: false,
  elementType: null,
  groupId: null,
  groupNumber: '',
  itemId: null,
  itemNumber: 0,
};

export interface OpenGroupModalParams {
  type: 'group';
  groupId: string;
  groupNumber: string;
}

export interface OpenItemModalParams {
  type: 'item';
  groupId: string;
  itemId: string;
  itemNumber: number;
}

export type OpenModalParams = OpenGroupModalParams | OpenItemModalParams;

export const useEstimateModal = () => {
  const [state, setState] = useState<ModalState>(CLOSED);

  const openModal = useCallback((params: OpenModalParams) => {
    setState({
      isOpen: true,
      elementType: params.type,
      groupId: params.groupId,
      groupNumber: params.type === 'group' ? params.groupNumber : '',
      itemId: params.type === 'item' ? params.itemId : null,
      itemNumber: params.type === 'item' ? params.itemNumber : 0,
    });
  }, []);

  const closeModal = useCallback(() => setState(CLOSED), []);

  return { ...state, openModal, closeModal };
};
