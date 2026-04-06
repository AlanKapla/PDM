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
  const initials = `${firstName[0]}${lastName[0]}`.toUpperCase();

  return (
    <Avatar 
      size={size} 
      bg={bg} 
      color={color} 
      name={initials}
      {...props}
    >
      {initials}
    </Avatar>
  );
});

export default UserAvatar;
