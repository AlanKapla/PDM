import React, { useCallback, useEffect } from "react";
import {
  Box,
  ButtonGroup,
  Divider,
  HStack,
  IconButton,
  Tooltip,
  useColorModeValue,
} from "@chakra-ui/react";
import { EditorContent, useEditor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Link from "@tiptap/extension-link";
import Placeholder from "@tiptap/extension-placeholder";
import {
  Bold,
  Heading2,
  Italic,
  Link2,
  List,
  ListOrdered,
  Strikethrough,
} from "lucide-react";

export interface ColdMailBodyEditorProps {
  value: string;
  onChange: (html: string) => void;
  isInvalid?: boolean;
  "aria-describedby"?: string;
}

export function ColdMailBodyEditor({
  value,
  onChange,
  isInvalid = false,
  "aria-describedby": ariaDescribedBy,
}: ColdMailBodyEditorProps): React.ReactElement {
  const borderColor = useColorModeValue("gray.200", "gray.600");
  const invalidBorder = useColorModeValue("red.500", "red.300");
  const toolbarBg = useColorModeValue("gray.50", "gray.700");
  const editorBg = useColorModeValue("white", "gray.800");

  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: { levels: [2] },
      }),
      Link.configure({
        openOnClick: false,
        HTMLAttributes: {
          rel: "noopener noreferrer",
          target: "_blank",
        },
      }),
      Placeholder.configure({
        placeholder: "Treść wiadomości…",
      }),
    ],
    content: value || "",
    immediatelyRender: false,
    onUpdate: ({ editor: current }) => {
      onChange(current.getHTML());
    },
    editorProps: {
      attributes: {
        role: "textbox",
        "aria-multiline": "true",
        "aria-label": "Treść wiadomości",
        ...(ariaDescribedBy ? { "aria-describedby": ariaDescribedBy } : {}),
        class: "cold-mail-body-editor",
      },
    },
  });

  useEffect(() => {
    if (!editor) {
      return;
    }
    if (value === "" && editor.getText().trim().length > 0) {
      editor.commands.clearContent(true);
    }
  }, [editor, value]);

  const setLink = useCallback((): void => {
    if (!editor) {
      return;
    }
    const previous: string = editor.getAttributes("link").href ?? "";
    const url: string | null = window.prompt("Adres URL linku", previous);
    if (url === null) {
      return;
    }
    const trimmed: string = url.trim();
    if (trimmed.length === 0) {
      editor.chain().focus().extendMarkRange("link").unsetLink().run();
      return;
    }
    editor
      .chain()
      .focus()
      .extendMarkRange("link")
      .setLink({ href: trimmed })
      .run();
  }, [editor]);

  if (!editor) {
    return <Box minH="200px" borderWidth="1px" borderRadius="md" />;
  }

  return (
    <Box
      borderWidth="1px"
      borderColor={isInvalid ? invalidBorder : borderColor}
      borderRadius="md"
      overflow="hidden"
      bg={editorBg}
    >
      <HStack
        spacing={1}
        px={2}
        py={1.5}
        bg={toolbarBg}
        borderBottomWidth="1px"
        borderColor={borderColor}
        flexWrap="wrap"
        role="toolbar"
        aria-label="Formatowanie treści"
      >
        <ButtonGroup size="sm" isAttached variant="ghost">
          <Tooltip label="Nagłówek">
            <IconButton
              aria-label="Nagłówek"
              icon={<Heading2 size={16} aria-hidden="true" />}
              isActive={editor.isActive("heading", { level: 2 })}
              onClick={() =>
                editor.chain().focus().toggleHeading({ level: 2 }).run()
              }
            />
          </Tooltip>
          <Tooltip label="Pogrubienie">
            <IconButton
              aria-label="Pogrubienie"
              icon={<Bold size={16} aria-hidden="true" />}
              isActive={editor.isActive("bold")}
              onClick={() => editor.chain().focus().toggleBold().run()}
            />
          </Tooltip>
          <Tooltip label="Kursywa">
            <IconButton
              aria-label="Kursywa"
              icon={<Italic size={16} aria-hidden="true" />}
              isActive={editor.isActive("italic")}
              onClick={() => editor.chain().focus().toggleItalic().run()}
            />
          </Tooltip>
          <Tooltip label="Przekreślenie">
            <IconButton
              aria-label="Przekreślenie"
              icon={<Strikethrough size={16} aria-hidden="true" />}
              isActive={editor.isActive("strike")}
              onClick={() => editor.chain().focus().toggleStrike().run()}
            />
          </Tooltip>
        </ButtonGroup>

        <Divider orientation="vertical" h="24px" />

        <ButtonGroup size="sm" isAttached variant="ghost">
          <Tooltip label="Lista punktowana">
            <IconButton
              aria-label="Lista punktowana"
              icon={<List size={16} aria-hidden="true" />}
              isActive={editor.isActive("bulletList")}
              onClick={() => editor.chain().focus().toggleBulletList().run()}
            />
          </Tooltip>
          <Tooltip label="Lista numerowana">
            <IconButton
              aria-label="Lista numerowana"
              icon={<ListOrdered size={16} aria-hidden="true" />}
              isActive={editor.isActive("orderedList")}
              onClick={() => editor.chain().focus().toggleOrderedList().run()}
            />
          </Tooltip>
          <Tooltip label="Link">
            <IconButton
              aria-label="Link"
              icon={<Link2 size={16} aria-hidden="true" />}
              isActive={editor.isActive("link")}
              onClick={setLink}
            />
          </Tooltip>
        </ButtonGroup>
      </HStack>

      <Box
        px={3}
        py={2}
        minH="200px"
        sx={{
          ".cold-mail-body-editor": {
            outline: "none",
            minH: "180px",
            fontSize: "sm",
            lineHeight: "tall",
            color: "neutral.800",
          },
          ".cold-mail-body-editor p": {
            my: 2,
          },
          ".cold-mail-body-editor h2": {
            fontSize: "lg",
            fontWeight: "bold",
            my: 3,
          },
          ".cold-mail-body-editor ul, .cold-mail-body-editor ol": {
            pl: 5,
            my: 2,
          },
          ".cold-mail-body-editor a": {
            color: "primary.600",
            textDecoration: "underline",
          },
          ".cold-mail-body-editor p.is-editor-empty:first-of-type::before": {
            color: "neutral.500",
            content: "attr(data-placeholder)",
            float: "left",
            height: 0,
            pointerEvents: "none",
          },
        }}
      >
        <EditorContent editor={editor} />
      </Box>
    </Box>
  );
}

/** TipTap empty doc is often `<p></p>` — treat as empty for validation. */
export function isColdMailBodyEmpty(html: string): boolean {
  const text: string = html
    .replace(/<[^>]*>/g, " ")
    .replace(/&nbsp;/gi, " ")
    .replace(/\s+/g, " ")
    .trim();
  return text.length === 0;
}
