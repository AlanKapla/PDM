import { memo } from "react";
import { Avatar } from "@chakra-ui/react";
import type { AvatarProps } from "@chakra-ui/react";

interface UserAvatarProps extends Omit<AvatarProps, 'name'> {
  firstName: string;
  lastName: string;
}

const UserAvatar = memo(function UserAvatar({ 
  firstName, 
  lastName, 
  size = "sm",
  bg = "primary.600",
  color = "white",
  ...props 
}: UserAvatarProps) {
  const fullName = `${firstName} ${lastName}`.trim();

  return (
    <Avatar 
      size={size} 
      bg={bg} 
      color={color} 
      name={fullName}
      {...props}
    />
  );
});

export default UserAvatar;
